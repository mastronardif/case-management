using System.Text.Json;
using System.Text.Json.Nodes;
using CaseManagement.SessionBillResolvers.V2.Reports;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class Claim837PStep(ICaseManagementRepository repository, ILogger<Claim837PStep> logger) : IWorkflowStep
{
    public string Operator => "claim837P";

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var caseId    = wfParams!["caseId"].GetInt32();
        var sessionId = wfParams!["sessionId"].GetInt32();

        logger.LogInformation("837P claim build. CaseId={CaseId}, SessionId={SessionId}, RunId={RunId}",
            caseId, sessionId, runId);

        var data  = await repository.Get837PDataAsync(caseId, sessionId, ct);
        var claim = BuildClaimJson(data, caseId, sessionId, runId);
        var html  = Claim837PReviewRenderer.Render(claim, runId);

        var indented = new JsonSerializerOptions { WriteIndented = true };

        var claimDocId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId, SessionId: sessionId),
            claim.ToJsonString(indented),
            "claim837P", "claim837P.json", "application/json", ct);

        var htmlDocId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId, SessionId: sessionId),
            html, "claim837P-review", "claim837P-review.html", "text/html", ct);

        logger.LogInformation("837P saved. claimDocId={ClaimDocId}, reviewDocId={ReviewDocId}",
            claimDocId, htmlDocId);

        return [claimDocId, htmlDocId];
    }

    private static JsonObject BuildClaimJson(Claim837PData data, int caseId, int sessionId, string runId)
    {
        var obj = new JsonObject
        {
            ["runId"]       = runId,
            ["generatedAt"] = DateTime.UtcNow.ToString("O"),
            ["caseId"]      = caseId,
            ["sessionId"]   = sessionId,
        };

        if (data.Session is not null) obj["session"] = data.Session.DeepClone().AsObject();
        if (data.Case    is not null) obj["case"]    = data.Case.DeepClone().AsObject();

        obj["coverage"]      = CloneArray(data.Coverage);
        obj["authorization"] = CloneArray(data.Authorization);
        obj["diagnoses"]     = CloneArray(data.Diagnoses);
        obj["provider"]      = CloneArray(data.Provider);

        if (data.Definition is not null) obj["definition"] = data.Definition.DeepClone();

        return obj;
    }

    private static JsonArray CloneArray(IReadOnlyList<JsonObject> rows)
    {
        var arr = new JsonArray();
        foreach (var row in rows)
            arr.Add(row.DeepClone());
        return arr;
    }
}
