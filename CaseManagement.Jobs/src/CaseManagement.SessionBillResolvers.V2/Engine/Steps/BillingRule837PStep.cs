using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// (Q) — applies the 837P rule to an assembled claim.
// Inputs: [claimDoc, ruleDoc]
// Loads session + auth docs from claim metadata, builds Loop 2400 entries,
// calculates billing, validates, saves updated claim.
public class BillingRule837PStep(ICaseManagementRepository repository, ILogger<BillingRule837PStep> logger) : IWorkflowStep
{
    public string Operator => "billingRule837P";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("billingRule837P", []);

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var claimDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Claim doc {inputDocIds[0]} not found");

        var ruleDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Rule doc {inputDocIds[1]} not found");

        int? caseId = wfParams?.TryGetValue("caseId", out var ci) == true && ci.ValueKind == JsonValueKind.Number
            ? ci.GetInt32() : null;

        logger.LogInformation("BillingRule837P started. claimDocId={C} ruleDocId={R}",
            inputDocIds[0], inputDocIds[1]);

        var result = await BillingRule837PEvaluator.EvaluateAsync(
            claimDoc.Content,
            ruleDoc.Content,
            async docId =>
            {
                var doc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: docId), ct);
                return doc?.Content;
            });

        if (result.ValidationIssues.Count > 0)
            logger.LogWarning("BillingRule837P validation issues ({Count}): {Issues}",
                result.ValidationIssues.Count, string.Join(" | ", result.ValidationIssues));

        var updatedJson = result.UpdatedClaim.ToJsonString(WriteIndented);
        var docId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            updatedJson, "claim837P", "claim837P.json", "application/json", ct);

        logger.LogInformation("BillingRule837P complete. outputDocId={D}, Issues={I}",
            docId, result.ValidationIssues.Count);

        return [docId];
    }
}
