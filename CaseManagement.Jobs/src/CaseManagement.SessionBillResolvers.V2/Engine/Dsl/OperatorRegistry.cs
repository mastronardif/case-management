using System.Text.Json;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Dsl;

public record OperatorToken(string Token, string Operator, string ReadsAs);

public class OperatorRegistry
{
    private readonly Dictionary<string, OperatorToken> _byToken;

    public OperatorRegistry(IEnumerable<OperatorToken> tokens)
    {
        _byToken = tokens.ToDictionary(t => t.Token, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryResolve(string token, out OperatorToken op) =>
        _byToken.TryGetValue(token, out op!);

    public static OperatorRegistry FromJson(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Supports two formats:
        //   Object format: { "tokens": [ { "token": "P", "operator": "projector", "readsAs": "project" } ] }
        //   Array format:  [ { "token": "P", "operator": "projector", "readsAs": "project" } ]
        //   (array format is what --list-operators --html produces when Token field is populated)
        var array = root.ValueKind == JsonValueKind.Array
            ? root
            : root.GetProperty("tokens");

        var tokens = array.EnumerateArray()
            .Where(t => t.TryGetProperty("token", out var tok) && !string.IsNullOrWhiteSpace(tok.GetString()))
            .Select(t => new OperatorToken(
                t.GetProperty("token").GetString()!,
                t.GetProperty("operator").GetString()!,
                t.TryGetProperty("readsAs", out var r) ? r.GetString() ?? "" : ""));

        return new OperatorRegistry(tokens);
    }
}
