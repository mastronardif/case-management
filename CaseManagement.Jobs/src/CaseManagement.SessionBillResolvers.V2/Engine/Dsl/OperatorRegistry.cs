using System.Text.Json;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Dsl;

public record OperatorTokenParam(string Name, string Type, bool Required, string Description);

public record OperatorToken(
    string Token,
    string Operator,
    string ReadsAs,
    string?               Description    = null,
    string[]?             InputLabels    = null,
    string[]?             OutputLabels   = null,
    string?               AlgebraExample = null,
    string?               CliExample     = null,
    OperatorTokenParam[]? Params         = null);

public class OperatorRegistry
{
    private readonly Dictionary<string, OperatorToken> _byToken;

    public IReadOnlyCollection<OperatorToken> All => _byToken.Values;

    public OperatorRegistry(IEnumerable<OperatorToken> tokens)
    {
        _byToken = tokens.ToDictionary(t => t.Token, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryResolve(string token, out OperatorToken op) =>
        _byToken.TryGetValue(token, out op!);

    public OperatorRegistry MergeWith(IEnumerable<OperatorToken> additionalTokens)
    {
        var merged = _byToken.Values.Concat(additionalTokens)
            .GroupBy(t => t.Token, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();

        return new OperatorRegistry(merged);
    }

    public static IReadOnlyCollection<OperatorToken> BuiltInTokens { get; } =
    [
        new OperatorToken("W", "x12Writer", "x12Writer", "Serialize a document to an X12 text file")
    ];

    // Handles both:
    //   Object format: { "tokens": [ { "token": "P", ... } ] }
    //   Array format:  [ { "token": "P", ... } ]
    public static OperatorRegistry FromJson(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root  = doc.RootElement;
        var array = root.ValueKind == JsonValueKind.Array ? root : root.GetProperty("tokens");

        var tokens = array.EnumerateArray()
            .Where(t => t.TryGetProperty("token", out var tok) && !string.IsNullOrWhiteSpace(tok.GetString()))
            .Select(t => new OperatorToken(
                Token:          t.GetProperty("token").GetString()!,
                Operator:       t.GetProperty("operator").GetString()!,
                ReadsAs:        t.TryGetProperty("readsAs",        out var r)  ? r.GetString()  ?? "" : "",
                Description:    t.TryGetProperty("description",    out var d)  ? d.GetString()        : null,
                InputLabels:    t.TryGetProperty("inputLabels",    out var il) ? ReadStringArray(il)   : null,
                OutputLabels:   t.TryGetProperty("outputLabels",   out var ol) ? ReadStringArray(ol)   : null,
                AlgebraExample: t.TryGetProperty("algebraExample", out var ae) ? ae.GetString()        : null,
                CliExample:     t.TryGetProperty("cliExample",     out var ce) ? ce.GetString()        : null,
                Params:         t.TryGetProperty("params",         out var ps) ? ReadParams(ps)        : null));

        return new OperatorRegistry(tokens);
    }

    private static string[] ReadStringArray(JsonElement el) =>
        el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().Select(e => e.GetString() ?? "").ToArray()
            : [];

    private static OperatorTokenParam[] ReadParams(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Array) return [];
        return el.EnumerateArray().Select(p => new OperatorTokenParam(
            Name:        p.TryGetProperty("name",        out var n) ? n.GetString() ?? "" : "",
            Type:        p.TryGetProperty("type",        out var tp) ? tp.GetString() ?? "" : "string",
            Required:    p.TryGetProperty("required",    out var rq) && rq.GetBoolean(),
            Description: p.TryGetProperty("description", out var ds) ? ds.GetString() ?? "" : "")).ToArray();
    }
}
