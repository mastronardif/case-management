using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// (AM) — smart add-merge: type-aware accumulation into target document. Creates a new document.
//   Objects  → deep merge (source fields override matching target fields, recursively)
//   Arrays   → find matching element by sessionId/documentId/id key: replace if found, append if not
//   Numbers  → add (accumulate)
//   Strings  → override
// After merging, recalculates loops.2300.CLM.CLM02 as the sum of all loops.2400[*].SV1.SV102
// when the target is a 837P-structured document (contains loops.2400 array).
public class AddMergeStep(ICaseManagementRepository repository, ILogger<AddMergeStep> logger) : IWorkflowStep
{
    public string Operator => "addMerge";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("addMerge",
        [
            new("caseId", "int", false, "CaseId for document context")
        ]);

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

    // Keys tried in order when matching array elements
    private static readonly string[] MatchKeys = ["sessionId", "documentId", "id"];

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var sourceDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"AddMerge source doc {inputDocIds[0]} not found");

        var targetDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"AddMerge target doc {inputDocIds[1]} not found");

        int? caseId = wfParams?.TryGetValue("caseId", out var ci) == true && ci.ValueKind == JsonValueKind.Number
            ? ci.GetInt32() : null;

        var source = JsonNode.Parse(sourceDoc.Content) ?? throw new InvalidOperationException("Source doc is not valid JSON");
        var target = JsonNode.Parse(targetDoc.Content) ?? throw new InvalidOperationException("Target doc is not valid JSON");

        var result = SmartMerge(target, source);

        // Recalculate CLM02 when target is an 837P document with service lines
        RecalculateClm02(result);

        // Renumber LX01 across all service lines
        RenumberServiceLines(result);

        var resultJson = result.ToJsonString(WriteIndented);

        var docId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            resultJson, "addMerged", "addMerged.json", "application/json", ct);

        logger.LogInformation("AddMerge complete. sourceDocId={S} targetDocId={T} → newDocId={D}", inputDocIds[0], inputDocIds[1], docId);
        return [docId];
    }

    // Smart merge: objects recurse, arrays smart-append, numbers accumulate, others override
    internal static JsonNode SmartMerge(JsonNode? target, JsonNode? source)
    {
        if (source is null) return target?.DeepClone() ?? JsonValue.Create(0)!;
        if (target is null) return source.DeepClone();

        if (source is JsonObject srcObj && target is JsonObject tgtObj)
        {
            var result = (JsonObject)tgtObj.DeepClone();
            foreach (var kvp in srcObj)
            {
                if (kvp.Value is null) { result[kvp.Key] = null; continue; }

                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value.DeepClone();
                }
                else if (result[kvp.Key] is JsonObject && kvp.Value is JsonObject)
                {
                    result[kvp.Key] = SmartMerge(result[kvp.Key], kvp.Value);
                }
                else if (result[kvp.Key] is JsonArray tgtArr && kvp.Value is JsonArray srcArr)
                {
                    result[kvp.Key] = SmartMergeArray(tgtArr, srcArr);
                }
                else if (result[kvp.Key] is JsonArray tgtArr2 && kvp.Value is JsonObject srcObj2)
                {
                    // Single source object into an array target → treat as array of one
                    result[kvp.Key] = SmartMergeArray(tgtArr2, new JsonArray(srcObj2.DeepClone()));
                }
                else if (IsNumeric(result[kvp.Key]) && IsNumeric(kvp.Value))
                {
                    result[kvp.Key] = JsonValue.Create(GetDouble(result[kvp.Key]) + GetDouble(kvp.Value));
                }
                else
                {
                    result[kvp.Key] = kvp.Value.DeepClone();
                }
            }
            return result;
        }

        // Source is array, target is not — replace
        return source.DeepClone();
    }

    private static JsonArray SmartMergeArray(JsonArray target, JsonArray source)
    {
        var result = (JsonArray)target.DeepClone();

        foreach (var srcElem in source)
        {
            if (srcElem is not JsonObject srcObj) { result.Add(srcElem?.DeepClone()); continue; }

            var matchIdx = FindMatchIndex(result, srcObj);
            if (matchIdx >= 0)
                result[matchIdx] = srcObj.DeepClone();
            else
                result.Add(srcObj.DeepClone());
        }

        return result;
    }

    private static int FindMatchIndex(JsonArray arr, JsonObject candidate)
    {
        foreach (var key in MatchKeys)
        {
            if (!candidate.ContainsKey(key)) continue;
            var candidateVal = candidate[key]?.ToString();
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is JsonObject elem && elem.ContainsKey(key) && elem[key]?.ToString() == candidateVal)
                    return i;
            }
        }
        return -1;
    }

    // loops.2300.CLM.CLM02 = sum of all loops.2400[*].SV1.SV102
    private static void RecalculateClm02(JsonNode result)
    {
        if (result is not JsonObject root) return;
        if (root["loops"] is not JsonObject loops) return;
        if (loops["2400"] is not JsonArray lines2400) return;
        if (loops["2300"] is not JsonObject loop2300) return;
        if (loop2300["CLM"] is not JsonObject clm) return;

        var total = lines2400
            .OfType<JsonObject>()
            .Where(l => l["SV1"] is JsonObject sv1 && sv1["SV102"] is not null)
            .Sum(l => GetDouble(((JsonObject)l["SV1"]!)["SV102"]));

        clm["CLM02"] = JsonValue.Create(total);
    }

    // Renumber LX.LX01 sequentially (1-based) across all 2400 service lines
    private static void RenumberServiceLines(JsonNode result)
    {
        if (result is not JsonObject root) return;
        if (root["loops"] is not JsonObject loops) return;
        if (loops["2400"] is not JsonArray lines2400) return;

        int seq = 1;
        foreach (var line in lines2400.OfType<JsonObject>())
        {
            if (line["LX"] is JsonObject lx)
                lx["LX01"] = JsonValue.Create(seq);
            seq++;
        }
    }

    private static bool IsNumeric(JsonNode? node) =>
        node is JsonValue v && (v.TryGetValue<double>(out _) || v.TryGetValue<int>(out _) || v.TryGetValue<decimal>(out _));

    private static double GetDouble(JsonNode? node)
    {
        if (node is JsonValue v)
        {
            if (v.TryGetValue<double>(out var d))  return d;
            if (v.TryGetValue<int>(out var i))     return i;
            if (v.TryGetValue<decimal>(out var m)) return (double)m;
        }
        return 0;
    }
}
