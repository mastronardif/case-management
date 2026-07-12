using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NCalc;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public record BillingRule837PResult(
    JsonObject UpdatedClaim,
    IReadOnlyList<string> ValidationIssues);

// Evaluates the 837P rule format:
//   constants      → X12 structural constants (HC, UN)
//   loadDocuments  → load authorization doc from claim metadata
//   forEach        → iterate metadata.sources.sessions, load each session doc, build Loop 2400 entries
//   rules          → document-level post-calculations (CLM02)
//   validations    → claim integrity checks
public static class BillingRule837PEvaluator
{
    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

    public static async Task<BillingRule837PResult> EvaluateAsync(
        string claimJson,
        string ruleJson,
        Func<int, Task<string?>> loadDocById)
    {
        var claim  = JsonNode.Parse(claimJson)!.AsObject();
        var rule   = JsonNode.Parse(ruleJson)!.AsObject();
        var issues = new List<string>();

        // 1. Flatten constants → "constants_serviceQualifier", "constants_unitCode", etc.
        var baseCtx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        FlattenInto(rule["constants"] as JsonObject, "constants", baseCtx);

        // 2. Load external documents declared in "loadDocuments"
        var externalDocs = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        if (rule["loadDocuments"] is JsonObject loadSection)
        {
            foreach (var (alias, pathNode) in loadSection)
            {
                var path = pathNode?.ToString();
                if (path is null) continue;

                var docIdNode = GetByPath(claim, path);
                if (docIdNode is null) continue;

                if (!int.TryParse(docIdNode.ToString(), out var docId)) continue;

                var docJson = await loadDocById(docId);
                if (docJson is not null)
                {
                    var doc = JsonNode.Parse(docJson)!.AsObject();
                    externalDocs[alias] = doc;
                    FlattenInto(doc, alias, baseCtx);
                }
            }
        }

        // 3. forEach — iterate sessions, load each, build Loop 2400 entries
        if (rule["forEach"] is JsonObject forEachDef)
        {
            var sourceStr = forEachDef["source"]?.ToString()   ?? "";
            var loadKey   = forEachDef["loadDoc"]?.ToString()  ?? "documentId";
            var appendTo  = forEachDef["appendTo"]?.ToString() ?? "";
            var fields    = forEachDef["fields"] as JsonArray;

            var sourceArr = GetByPath(claim, sourceStr) as JsonArray;
            if (sourceArr is not null && fields is not null)
            {
                var targetArr = EnsureArray(claim, appendTo);

                for (int i = 0; i < sourceArr.Count; i++)
                {
                    var entry = sourceArr[i]?.AsObject();
                    if (entry is null) continue;

                    var docIdVal = entry[loadKey];
                    if (docIdVal is null || !int.TryParse(docIdVal.ToString(), out var docId))
                    {
                        issues.Add($"forEach[{i}]: missing or invalid '{loadKey}'");
                        continue;
                    }

                    var sessionJson = await loadDocById(docId);
                    if (sessionJson is null)
                    {
                        issues.Add($"Session doc {docId} not found");
                        continue;
                    }

                    var sessionDoc = JsonNode.Parse(sessionJson)!.AsObject();

                    // Build per-element context: base + session doc fields + documentId
                    var ctx = new Dictionary<string, object?>(baseCtx, StringComparer.OrdinalIgnoreCase);
                    FlattenInto(sessionDoc, null, ctx);
                    ctx["documentId"] = (object)docId;

                    var element = new JsonObject();

                    foreach (var fieldDef in fields)
                    {
                        var fd     = fieldDef!.AsObject();
                        var target = fd["target"]?.ToString() ?? "";
                        var ftype  = fd["type"]?.ToString();

                        JsonNode? value;

                        if (ftype == "literal")
                        {
                            value = fd["value"]?.DeepClone();
                        }
                        else if (fd.ContainsKey("source") && fd["source"] is not null)
                        {
                            var srcPath = fd["source"]!.ToString();
                            value = GetByPath(sessionDoc, srcPath)?.DeepClone();
                        }
                        else if (fd.ContainsKey("expression") && fd["expression"] is not null)
                        {
                            var exprStr = fd["expression"]!.ToString();
                            try
                            {
                                var result = Eval(exprStr, ctx, claim);
                                value = ToNode(result);
                                // Feed computed result back into context
                                var nk = NormKey(target);
                                ctx[nk] = ToScalar(result);
                                // Also register leaf name for convenience (SV1.SV104 → SV104)
                                var leaf = target.Split('.').Last();
                                if (!ctx.ContainsKey(leaf)) ctx[leaf] = ToScalar(result);
                            }
                            catch (Exception ex)
                            {
                                issues.Add($"forEach field '{target}': {ex.Message}");
                                value = null;
                            }
                        }
                        else
                        {
                            value = null;
                        }

                        // Feed any value into context under normalised key
                        ctx[NormKey(target)] = ToScalar(value);

                        SetElementPath(element, target, value);
                    }

                    targetArr.Add(element);
                }
            }
        }

        // 4. Document-level rules (e.g. CLM02 = sum)
        if (rule["rules"] is JsonArray rulesArr)
        {
            var ctx = new Dictionary<string, object?>(baseCtx, StringComparer.OrdinalIgnoreCase);
            foreach (var ruleItem in rulesArr)
            {
                var ri    = ruleItem!.AsObject();
                var field = ri["field"]?.ToString() ?? "";
                var expr  = ri["expression"]?.ToString() ?? "";
                try
                {
                    var result = Eval(expr, ctx, claim);
                    SetByPath(claim, field, ToNode(result));
                }
                catch (Exception ex)
                {
                    issues.Add($"Rule '{field}': {ex.Message}");
                }
            }
        }

        // 5. Validations
        if (rule["validations"] is JsonArray validationsArr)
        {
            foreach (var v in validationsArr)
            {
                var vd      = v!.AsObject();
                var field   = vd["field"]?.ToString() ?? "";
                var expr    = vd["expression"]?.ToString() ?? "";
                var message = vd["message"]?.ToString() ?? $"Validation failed: {field}";

                foreach (var val in GetFieldValues(claim, field))
                {
                    var ctx = new Dictionary<string, object?>(baseCtx, StringComparer.OrdinalIgnoreCase)
                        { ["value"] = val };
                    try
                    {
                        var pass = Eval(expr, ctx, claim);
                        if (pass is bool b && !b) { issues.Add(message); break; }
                    }
                    catch { /* skip malformed validation */ }
                }
            }
        }

        return new BillingRule837PResult(claim, issues);
    }

    // ── NCalc evaluation ─────────────────────────────────────────────────────

    private static object? Eval(string expression, Dictionary<string, object?> ctx, JsonObject? claim = null)
    {
        // Pre-process sum(path) aggregate → numeric literal before NCalc sees it
        expression = Regex.Replace(expression, @"sum\(([^)]+)\)", m =>
        {
            var path  = m.Groups[1].Value.Trim().Trim('"', '\'');
            var total = SumPath(claim, path);
            return total.ToString("G");
        });

        // Normalise dotted references: authorization.unitRate → authorization_unitRate
        var normalised = Regex.Replace(expression, @"\b([A-Za-z_]\w*)\.(\w+)\b", "$1_$2");

        // Also normalise keys in context for lookup
        var flatCtx = ctx.ToDictionary(
            kv => NormKey(kv.Key),
            kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);

        var expr = new Expression(normalised, ExpressionOptions.AllowNullParameter);

        expr.EvaluateParameter += (name, args) =>
        {
            if (flatCtx.TryGetValue(name, out var v)) args.Result = v;
        };

        expr.EvaluateFunction += (name, args) =>
        {
            switch (name.ToLowerInvariant())
            {
                case "minutes":
                {
                    var s = args.Parameters[0].Evaluate()?.ToString() ?? "";
                    var e = args.Parameters[1].Evaluate()?.ToString() ?? "";
                    args.Result = MinutesBetween(s, e);
                    break;
                }
                case "ceiling":
                    args.Result = (double)Math.Ceiling(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "floor":
                    args.Result = (double)Math.Floor(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "round":
                {
                    var val  = Convert.ToDouble(args.Parameters[0].Evaluate());
                    var dec  = args.Parameters.Length > 1 ? Convert.ToInt32(args.Parameters[1].Evaluate()) : 2;
                    args.Result = Math.Round(val, dec, MidpointRounding.AwayFromZero);
                    break;
                }
                case "length":
                    args.Result = args.Parameters[0].Evaluate()?.ToString()?.Length ?? 0;
                    break;
                case "notempty":
                    args.Result = !string.IsNullOrWhiteSpace(args.Parameters[0].Evaluate()?.ToString());
                    break;
            }
        };

        return expr.Evaluate();
    }

    // ── Path helpers ─────────────────────────────────────────────────────────

    // Navigate a dot-bracket path in a JsonObject
    private static JsonNode? GetByPath(JsonNode? node, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return node;
        JsonNode? current = node;
        foreach (var part in path.Split('.'))
        {
            if (current is null) return null;
            var b = part.IndexOf('[');
            if (b >= 0)
            {
                var key = part[..b];
                if (!int.TryParse(part[(b + 1)..part.IndexOf(']')], out var idx)) return null;
                current = (string.IsNullOrEmpty(key) ? current : current[key]);
                if (current is JsonArray ja) current = idx < ja.Count ? ja[idx] : null;
            }
            else
            {
                current = current[part];
            }
        }
        return current;
    }

    // Set a value at a nested path, creating intermediate objects/arrays
    private static void SetByPath(JsonObject root, string path, JsonNode? value)
    {
        var segments = ParseSegments(path);
        JsonNode current = root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            var (key, idx) = segments[i];
            var obj = (JsonObject)current;
            if (idx.HasValue)
            {
                if (obj[key] is not JsonArray arr) { arr = new JsonArray(); obj[key] = arr; }
                while (arr.Count <= idx.Value) arr.Add(new JsonObject());
                current = arr[idx.Value]!;
            }
            else
            {
                if (obj[key] is not JsonObject child) { child = new JsonObject(); obj[key] = child; }
                current = child;
            }
        }
        var (lk, li) = segments[^1];
        var last = (JsonObject)current;
        if (li.HasValue)
        {
            if (last[lk] is not JsonArray arr) { arr = new JsonArray(); last[lk] = arr; }
            while (arr.Count <= li.Value) arr.Add(JsonValue.Create(0)!);
            arr[li.Value] = value;
        }
        else
        {
            last[lk] = value;
        }
    }

    // Set path within a Loop 2400 element (no root, relative path)
    private static void SetElementPath(JsonObject element, string path, JsonNode? value)
    {
        // Strip "computed." prefix — computed fields live in element as-is for context but are stored
        SetByPath(element, path, value);
    }

    // Ensure an array exists at path, return it
    private static JsonArray EnsureArray(JsonObject root, string path)
    {
        var node = GetByPath(root, path);
        if (node is JsonArray existing) return existing;
        var arr = new JsonArray();
        SetByPath(root, path, arr);
        return arr;
    }

    // Resolve wildcard paths (loops.2400[*].SV1.SV104) → all matching values
    private static IEnumerable<object?> GetFieldValues(JsonObject claim, string path)
    {
        if (!path.Contains("[*]"))
        {
            yield return ToScalar(GetByPath(claim, path));
            yield break;
        }

        var parts    = path.Split("[*].", 2);
        var arrPath  = parts[0];
        var subPath  = parts.Length > 1 ? parts[1] : "";
        var arr      = GetByPath(claim, arrPath) as JsonArray;
        if (arr is null) yield break;

        foreach (var item in arr)
        {
            if (item is null) { yield return null; continue; }
            yield return string.IsNullOrEmpty(subPath)
                ? ToScalar(item)
                : ToScalar(GetByPath(item.AsObject(), subPath));
        }
    }

    // Sum values at wildcard path (loops.2400[*].SV1.SV102)
    private static double SumPath(JsonObject? claim, string path)
    {
        if (claim is null || !path.Contains("[*]")) return 0;
        return GetFieldValues(claim, path)
            .Where(v => v is not null)
            .Sum(v => Convert.ToDouble(v));
    }

    private static (string Key, int? Index)[] ParseSegments(string path) =>
        path.Split('.').Select(part =>
        {
            var b = part.IndexOf('[');
            if (b < 0) return (part, (int?)null);
            var key = part[..b];
            var idx = int.Parse(part[(b + 1)..part.IndexOf(']')]);
            return (key, (int?)idx);
        }).ToArray();

    // ── Context helpers ───────────────────────────────────────────────────────

    private static void FlattenInto(JsonObject? obj, string? prefix, Dictionary<string, object?> ctx)
    {
        if (obj is null) return;
        foreach (var (k, v) in obj)
        {
            var key = prefix is null ? k : $"{prefix}_{k}";
            if (v is JsonObject nested) FlattenInto(nested, key, ctx);
            else if (v is JsonArray)   { /* skip arrays */ }
            else ctx[key] = ToScalar(v);
        }
    }

    // Normalise dotted/bracketed path to underscore key: "SV1.SV104" → "SV1_SV104"
    private static string NormKey(string key) =>
        Regex.Replace(key, @"[\.\[\]]", "_").TrimEnd('_');

    // ── Type helpers ──────────────────────────────────────────────────────────

    private static object? ToScalar(JsonNode? node) => node switch
    {
        JsonValue jv when jv.TryGetValue<double>(out var d)  => d,
        JsonValue jv when jv.TryGetValue<int>(out var i)     => (double)i,
        JsonValue jv when jv.TryGetValue<bool>(out var b)    => b,
        JsonValue jv                                          => jv.ToString(),
        null                                                  => null,
        _                                                     => null
    };

    private static object? ToScalar(object? value) => value switch
    {
        double d  => d,
        float f   => (double)f,
        int i     => (double)i,
        long l    => (double)l,
        bool b    => b,
        string s  => s,
        null      => null,
        _         => value.ToString()
    };

    private static JsonNode? ToNode(object? value) => value switch
    {
        double d  => JsonValue.Create(d),
        float f   => JsonValue.Create((double)f),
        int i     => JsonValue.Create((double)i),
        long l    => JsonValue.Create((double)l),
        bool b    => JsonValue.Create(b),
        string s  => JsonValue.Create(s),
        null      => null,
        _         => JsonValue.Create(value.ToString())
    };

    private static double MinutesBetween(string start, string end)
    {
        if (!TimeSpan.TryParse(start, out var s) || !TimeSpan.TryParse(end, out var e))
            return 0;
        var diff = e - s;
        if (diff < TimeSpan.Zero) diff = diff.Add(TimeSpan.FromHours(24));
        return diff.TotalMinutes;
    }
}
