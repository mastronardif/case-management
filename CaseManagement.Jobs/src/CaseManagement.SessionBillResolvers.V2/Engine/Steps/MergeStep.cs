using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// (M) — field-level merge: source fields override matching target fields; unmatched target fields preserved.
// Recurses into nested objects. Arrays in source replace arrays in target entirely.
// Creates a new document — never mutates the target.
public class MergeStep(ICaseManagementRepository repository, ILogger<MergeStep> logger) : IWorkflowStep
{
    public string Operator => "merge";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("merge",
        [
            new("caseId", "int", false, "CaseId for document context")
        ]);

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var sourceDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Merge source doc {inputDocIds[0]} not found");

        var targetDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Merge target doc {inputDocIds[1]} not found");

        int? caseId = wfParams?.TryGetValue("caseId", out var ci) == true && ci.ValueKind == JsonValueKind.Number
            ? ci.GetInt32() : null;

        var source = JsonNode.Parse(sourceDoc.Content) ?? throw new InvalidOperationException("Source doc is not valid JSON");
        var target = JsonNode.Parse(targetDoc.Content) ?? throw new InvalidOperationException("Target doc is not valid JSON");

        var merged    = DeepMerge(target, source);
        var mergedJson = merged.ToJsonString(WriteIndented);

        var docId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            mergedJson, "merged", "merged.json", "application/json", ct);

        logger.LogInformation("Merge complete. sourceDocId={S} targetDocId={T} → newDocId={D}", inputDocIds[0], inputDocIds[1], docId);
        return [docId];
    }

    // Recursively merges source into target. Source fields override; nested objects recurse; arrays replace.
    internal static JsonNode DeepMerge(JsonNode? target, JsonNode? source)
    {
        if (source is null) return target?.DeepClone() ?? JsonValue.Create(0)!;
        if (target is null) return source.DeepClone();

        if (source is JsonObject srcObj && target is JsonObject tgtObj)
        {
            var result = (JsonObject)tgtObj.DeepClone();
            foreach (var kvp in srcObj)
            {
                if (kvp.Value is null) { result[kvp.Key] = null; continue; }

                result[kvp.Key] = result.ContainsKey(kvp.Key) && result[kvp.Key] is JsonObject && kvp.Value is JsonObject
                    ? DeepMerge(result[kvp.Key], kvp.Value)
                    : kvp.Value.DeepClone();
            }
            return result;
        }

        return source.DeepClone();
    }
}
