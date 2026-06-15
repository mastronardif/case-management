using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class DocResolveStep(ICaseManagementRepository repository, ILogger<DocResolveStep> logger) : IWorkflowStep
{
    public string Operator => "docResolve";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var docId     = inputDocIds[0];
        var tableName = wfParams!["tableName"].GetString()!;
        var caseId    = wfParams!["caseId"].GetInt32();
        int? srcDocId  = wfParams.TryGetValue("srcDocId",  out var sd) && sd.ValueKind != JsonValueKind.Null
            ? sd.GetInt32() : null;

        logger.LogInformation("DocResolve. table={TableName} docId={DocId} caseId={CaseId} srcDocId={SrcDocId}",
            tableName, docId, caseId, srcDocId);

        await repository.ResolveDocAsync(docId, tableName, caseId, srcDocId, ct);

        var confirm = JsonSerializer.Serialize(new
        {
            resolved   = true,
            tableName,
            caseId,
            docId,
            runId,
            resolvedAt = DateTime.UtcNow.ToString("O")
        }, JsonOptions);

        var confirmDocId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            confirm, "docResolveConfirm", "confirm.json", "application/json", ct);

        logger.LogInformation("DocResolve complete. confirmDocId={ConfirmDocId}", confirmDocId);
        return [confirmDocId];
    }
}
