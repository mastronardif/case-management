using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public class BillingRuleStep(ICaseManagementRepository repository, ILogger<BillingRuleStep> logger) : IWorkflowStep
{
    public string StepType => "billingRule";

    public async Task<int> ExecuteAsync(int[] inputDocIds, CancellationToken ct)
    {
        var projectionDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Billing projection doc {inputDocIds[0]} not found");

        var billingRuleDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Billing rule doc {inputDocIds[1]} not found");

        // TODO: apply billingRule to projection to produce invoice
        var invoiceJson = projectionDoc.Content;

        var context = new DocumentContext(CaseId: projectionDoc.CaseId, SessionId: projectionDoc.SessionId);
        var docId = await repository.SaveDocumentAsync(context, invoiceJson, "billingResult", "billingResult.json", ct);

        logger.LogInformation("BillingRuleStep complete. OutputDocId: {DocId}", docId);
        return docId;
    }
}
