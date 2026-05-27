using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public class ProjectionStep(ICaseManagementRepository repository, ILogger<ProjectionStep> logger) : IWorkflowStep
{
    public string StepType => "projection";

    public async Task<int> ExecuteAsync(int[] inputDocIds, CancellationToken ct)
    {
        var sessionDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Session doc {inputDocIds[0]} not found");

        var projDefDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Projection definition doc {inputDocIds[1]} not found");

        var projection = ProjectionTransformer.Transform(projDefDoc.Content, sessionDoc.Content);
        var projectionJson = projection.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        var context = new DocumentContext(CaseId: sessionDoc.CaseId, SessionId: sessionDoc.SessionId);
        var docId = await repository.SaveDocumentAsync(context, projectionJson, "billingProjection", "billingProjection.json", ct);

        logger.LogInformation("ProjectionStep complete. OutputDocId: {DocId}", docId);
        return docId;
    }
}
