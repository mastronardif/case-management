using System.Text.Json;
using System.Text.Json.Nodes;
using CaseManagement.SessionBillResolvers.V2.Reports;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class Claim837PStep(ICaseManagementRepository repository, ILogger<Claim837PStep> logger) : IWorkflowStep
{
    public string Operator => "claim837P";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new(
        Operator:     "claim837P",
        Description:  "Builds a HIPAA 837P claim from session, case, coverage, authorization, diagnoses, provider, and assessment data. Produces a claim JSON and an HTML review doc.",
        InputLabels:  [],
        OutputLabels: ["claim837P.json", "claim837P-review.html"],
        Params:
        [
            new("caseId",    "int", true, "CaseId to pull claim data for"),
            new("sessionId", "int", true, "SessionId to pull claim data for")
        ]);

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
            ["runId"]         = runId,
            ["generatedAt"]   = DateTime.UtcNow.ToString("O"),
            ["caseId"]        = caseId,
            ["sessionId"]     = sessionId,
            ["session"]       = ToJsonArray(data.Session),
            ["case"]          = ToJsonArray(data.Case),
            ["coverage"]      = ToJsonArray(data.Coverage),
            ["authorization"] = ToJsonArray(data.Authorization),
            ["diagnoses"]     = ToJsonArray(data.Diagnoses),
            ["provider"]      = ToJsonArray(data.Provider),
            ["assessment"]    = ToJsonArray(data.Assessment),
            ["definition"]    = data.Definition,
        };

        return obj;
    }

    private static JsonArray ToJsonArray(IReadOnlyList<JsonObject> rows)
    {
        var arr = new JsonArray();
        foreach (var row in rows)
            arr.Add(row);
        return arr;
    }
}
