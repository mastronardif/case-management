using System.Text.Json;
using CaseManagement.SessionBillResolvers.V2.Reports;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// (Y) — renders an AvailityClaimForm_Preview doc as a self-contained HTML review page.
// Unary operator: takes one input (the Availity preview doc), produces HTML for eyeball verification.
public class AvailityHtmlRenderStep(ICaseManagementRepository repository, ILogger<AvailityHtmlRenderStep> logger) : IWorkflowStep
{
    public string Operator => "availityHtmlRender";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("availityHtmlRender", []);

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var previewDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"AvailityHtmlRender: preview doc {inputDocIds[0]} not found");

        int? caseId = wfParams?.TryGetValue("caseId", out var ci) == true && ci.ValueKind == JsonValueKind.Number
            ? ci.GetInt32() : null;

        var html = AvailityClaimFormRenderer.Render(previewDoc.Content);
        var htmlDocId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            html, "availityClaimReview", "Availity-Claim.html", "text/html", ct);

        logger.LogInformation("AvailityHtmlRender complete. previewDocId={P} → htmlDocId={D}",
            inputDocIds[0], htmlDocId);
        return [htmlDocId];
    }
}
