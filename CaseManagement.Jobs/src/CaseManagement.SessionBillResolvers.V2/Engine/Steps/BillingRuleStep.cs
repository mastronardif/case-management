using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class BillingRuleStep(ICaseManagementRepository repository, ILogger<BillingRuleStep> logger) : IWorkflowStep
{
    public string Operator => "billingRule";

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId, CancellationToken ct)
    {
        var projectionDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Billing projection doc {inputDocIds[0]} not found");

        var billingRuleDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Billing rule doc {inputDocIds[1]} not found");

        var evaluation  = BillingRuleEvaluator.Evaluate(projectionDoc.Content, billingRuleDoc.Content);
        var invoiceJson = evaluation.Invoice.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var context = new DocumentContext(CaseId: projectionDoc.CaseId, SessionId: projectionDoc.SessionId);
        var docId = await repository.SaveDocumentAsync(context, invoiceJson, "billingResult", "billingResult.json", "application/json", ct);

        logger.LogInformation("BillingRule complete. OutputDocId: {DocId}", docId);
        return new[] { docId };
    }
}
