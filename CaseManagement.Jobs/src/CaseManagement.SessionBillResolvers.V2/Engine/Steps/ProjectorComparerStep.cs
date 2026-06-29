using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class ProjectorComparerStep(
    ICaseManagementRepository repository,
    ProjectProcessor processor,
    ILogger<ProjectorComparerStep> logger) : IWorkflowStep
{
    public string Operator => "projectorComparer";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new(
        Operator:     "projectorComparer",
        Description:  "Validates a session extraction doc against a projection definition. Produces a field-by-field audit JSON and a human-readable HTML review.",
        InputLabels:  ["sessionDoc", "projectionDefinition"],
        OutputLabels: ["sessionAudit.json", "sessionReview.html"],
        Params:
        [
            new("tableName", "string", false, "Target [cases] table name"),
            new("caseId",    "int",    false, "CaseId for document context"),
            new("srcDocId",  "int",    false, "Original source document id")
        ]);

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId, IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
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

        var tableName = wfParams?.TryGetValue("tableName", out var tn)  == true ? tn.GetString()   : null;
        var caseId    = wfParams?.TryGetValue("caseId",    out var ci)  == true && ci.ValueKind  == JsonValueKind.Number ? ci.GetInt32()  : (int?)null;
        var srcDocId  = wfParams?.TryGetValue("srcDocId",  out var sd)  == true && sd.ValueKind  == JsonValueKind.Number ? sd.GetInt32()  : (int?)null;

        // JSON [0] = primary/machine-readable, HTML [1] = human review
        var auditJson  = ProjectProcessor.RenderAuditJson(runInput, result);
        var reviewHtml = processor.RenderReviewHtml(sessionDoc.Content, projDefDoc.Content, result, inputDocIds[0], tableName, caseId, srcDocId);

        var context = new DocumentContext(CaseId: sessionDoc.CaseId, SessionId: sessionDoc.SessionId);

        var auditDocId = await repository.SaveDocumentAsync(context, auditJson,  "sessionAudit",  "sessionAudit.json",  "application/json", ct);
        var htmlDocId  = await repository.SaveDocumentAsync(context, reviewHtml, "sessionReview", "sessionReview.html", "text/html",         ct);

        logger.LogInformation(
            "ProjectorComparer complete. Fields: {Total}, Issues: {Issues}. AuditDocId: {AuditId}, HtmlDocId: {HtmlId}",
            result.MappedFields.Count, result.ValidationIssues.Count, auditDocId, htmlDocId);

        return new[] { auditDocId, htmlDocId };
    }
}
