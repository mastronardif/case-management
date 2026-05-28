using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class ProjectorComparerStep(
    ICaseManagementRepository repository,
    ProjectProcessor processor,
    ILogger<ProjectorComparerStep> logger) : IWorkflowStep
{
    public string Operator => "projectorComparer";

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId, CancellationToken ct)
    {
        var sessionDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Session doc {inputDocIds[0]} not found");

        var projDefDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Projection definition doc {inputDocIds[1]} not found");

        var result = processor.Project(sessionDoc.Content, projDefDoc.Content);

        var runInput = new RunInput(
            RunId:                  runId,
            RunAction:              "projectorComparer",
            InputDoc:               inputDocIds[0],
            ProjectionDefinitionId: inputDocIds[1],
            BillingRuleId:          0,
            CaseNumber:             sessionDoc.CaseId,
            SessionNumber:          sessionDoc.SessionId,
            CreatedDate:            DateTime.UtcNow);

        // JSON [0] = primary/machine-readable, HTML [1] = human review
        var auditJson  = ProjectProcessor.RenderAuditJson(runInput, result);
        var reviewHtml = processor.RenderReviewHtml(sessionDoc.Content, projDefDoc.Content, result);

        var context = new DocumentContext(CaseId: sessionDoc.CaseId, SessionId: sessionDoc.SessionId);

        var auditDocId = await repository.SaveDocumentAsync(context, auditJson,  "sessionAudit",  "sessionAudit.json",  "application/json", ct);
        var htmlDocId  = await repository.SaveDocumentAsync(context, reviewHtml, "sessionReview", "sessionReview.html", "text/html",         ct);

        logger.LogInformation(
            "ProjectorComparer complete. Fields: {Total}, Issues: {Issues}. AuditDocId: {AuditId}, HtmlDocId: {HtmlId}",
            result.MappedFields.Count, result.ValidationIssues.Count, auditDocId, htmlDocId);

        return new[] { auditDocId, htmlDocId };
    }
}
