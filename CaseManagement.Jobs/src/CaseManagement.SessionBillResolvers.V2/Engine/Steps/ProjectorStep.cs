using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class ProjectorStep(ICaseManagementRepository repository, ILogger<ProjectorStep> logger) : IWorkflowStep
{
    public string Operator => "projector";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new(
        Operator:     "projector",
        Description:  "Transforms a session extraction doc using a projection definition into a billing projection.",
        InputLabels:  ["sessionDoc", "projectionDefinition"],
        OutputLabels: ["billingProjection.json"],
        Params:       []);

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId, IReadOnlyDictionary<string, System.Text.Json.JsonElement>? wfParams, CancellationToken ct)
    {
        var sessionDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"Session doc {inputDocIds[0]} not found");

        var projDefDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[1]), ct)
            ?? throw new InvalidOperationException($"Projection definition doc {inputDocIds[1]} not found");

        var projection = ProjectionTransformer.Transform(projDefDoc.Content, sessionDoc.Content);
        var projectionJson = projection.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        var context = new DocumentContext(CaseId: sessionDoc.CaseId, SessionId: sessionDoc.SessionId);
        var docId = await repository.SaveDocumentAsync(context, projectionJson, "billingProjection", "billingProjection.json", "application/json", ct);

        logger.LogInformation("Projector complete. OutputDocId: {DocId}", docId);
        return new[] { docId };
    }
}
