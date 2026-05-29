using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NCalc;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public record BillingEvaluationResult(
    JsonObject Invoice,
    IReadOnlyList<string> ValidationIssues);

public static class BillingRuleEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static BillingEvaluationResult Evaluate(string projectionJson, string billingRuleJson)
    {
        var projection = JsonNode.Parse(projectionJson)!.AsObject();
        var rule       = JsonNode.Parse(billingRuleJson)!.AsObject();

        var ruleName    = rule["name"]?.ToString()    ?? "unknown";
        var ruleVersion = rule["version"]?.ToString() ?? "0";

        // Load constants
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (rule["constants"] is JsonObject constantsObj)
        {
            foreach (var (key, val) in constantsObj)
                context[key] = ToDouble(val) ?? (object?)val?.ToString();
        }

        // Load projection fields — these are the @field references
        foreach (var (key, val) in projection)
            context[key] = ToDouble(val) ?? (object?)val?.ToString();

        // Validate required fields
        var issues = new List<string>();
        if (rule["requiredFields"] is JsonArray required)
        {
            foreach (var req in required)
            {
                var name = req!.ToString();
                if (!context.ContainsKey(name) || context[name] is null)
                    issues.Add($"Required field '{name}' is missing");
            }
        }

        // Pre-populate any @field references that aren't in the projection as null
        // so NCalc sees them as defined (null) rather than throwing "Parameter not defined"
        if (rule["lineItems"] is JsonArray itemsForScan)
        {
            foreach (var item in itemsForScan)
            {
                var expression = item!.AsObject()["expression"]!.ToString();
                foreach (System.Text.RegularExpressions.Match m in Regex.Matches(expression, @"@(\w+)"))
                {
                    var fieldName = m.Groups[1].Value;
                    if (!context.ContainsKey(fieldName))
                        context[fieldName] = null;
                }
            }
        }

        // Evaluate line items sequentially — each result feeds the next
        var lineItems = new JsonObject();
        if (rule["lineItems"] is JsonArray items)
        {
            foreach (var item in items)
            {
                var li         = item!.AsObject();
                var field      = li["field"]!.ToString();
                var expression = li["expression"]!.ToString();

                // @field → field  (strip @ prefix; value already in context from projection)
                var cleanExpr = Regex.Replace(expression, @"@(\w+)", "$1");

                try
                {
                    var result   = Eval(cleanExpr, context);
                    context[field] = result;
                    lineItems[field] = ToJsonValue(result);
                }
                catch (Exception ex)
                {
                    issues.Add($"Error in '{field}' ({expression}): {ex.Message}");
                    context[field]  = null;
                    lineItems[field] = null;
                }
            }
        }

        var invoice = new JsonObject
        {
            ["ruleName"]         = ruleName,
            ["ruleVersion"]      = ruleVersion,
            ["validationIssues"] = new JsonArray(issues.Select(i => JsonValue.Create(i)!).ToArray()),
            ["lineItems"]        = lineItems
        };

        return new BillingEvaluationResult(invoice, issues);
    }

    private static object? Eval(string expression, Dictionary<string, object?> context)
    {
        var expr = new Expression(expression, ExpressionOptions.AllowNullParameter);

        foreach (var (k, v) in context)
            expr.Parameters[k] = v;

        expr.EvaluateFunction += (name, args) =>
        {
            if (name.Equals("timediff", StringComparison.OrdinalIgnoreCase))
            {
                var start = args.Parameters[0].Evaluate()?.ToString() ?? "";
                var end   = args.Parameters[1].Evaluate()?.ToString() ?? "";
                args.Result = TimeDiffHours(start, end);
            }
            else if (name.Equals("floor", StringComparison.OrdinalIgnoreCase))
            {
                args.Result = Math.Floor(Convert.ToDouble(args.Parameters[0].Evaluate()));
            }
            else if (name.Equals("min", StringComparison.OrdinalIgnoreCase))
            {
                var a = Convert.ToDouble(args.Parameters[0].Evaluate());
                var b = Convert.ToDouble(args.Parameters[1].Evaluate());
                args.Result = Math.Min(a, b);
            }
        };

        return expr.Evaluate();
    }

    // Military HH:mm → decimal hours. Handles midnight crossing (e.g. 22:00 → 02:00 = 4h).
    private static double TimeDiffHours(string start, string end)
    {
        var s    = TimeSpan.Parse(start);
        var e    = TimeSpan.Parse(end);
        var diff = e - s;
        if (diff < TimeSpan.Zero) diff = diff.Add(TimeSpan.FromHours(24));
        return diff.TotalHours;
    }

    private static double? ToDouble(JsonNode? node)
    {
        if (node is JsonValue jv && jv.TryGetValue<double>(out var d)) return d;
        return null;
    }

    private static JsonNode? ToJsonValue(object? value) => value switch
    {
        double d  => JsonValue.Create(d),
        float f   => JsonValue.Create((double)f),
        int i     => JsonValue.Create((double)i),
        long l    => JsonValue.Create((double)l),
        string s  => JsonValue.Create(s),
        null      => null,
        _         => JsonValue.Create(value.ToString())
    };
}
