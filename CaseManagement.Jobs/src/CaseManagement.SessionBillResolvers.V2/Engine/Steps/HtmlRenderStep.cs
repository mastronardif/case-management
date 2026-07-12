using System.Text.Json;
using CaseManagement.SessionBillResolvers.V2.Reports;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// (X) — renders a boxes-format doc as a CMS-1500 HTML review page.
// Unary operator: takes one input (the boxes doc), produces HTML plus a JSON artifact of the source transform.
public class HtmlRenderStep(ICaseManagementRepository repository, ILogger<HtmlRenderStep> logger) : IWorkflowStep
{
    public string Operator => "htmlRender";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("htmlRender", []);

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var boxesDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"HtmlRender: boxes doc {inputDocIds[0]} not found");

        int? caseId = wfParams?.TryGetValue("caseId", out var ci) == true && ci.ValueKind == JsonValueKind.Number
            ? ci.GetInt32() : null;

        var html = Cms1500Renderer.Render(boxesDoc.Content);
        var htmlDocId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            html, "cms1500Review", "CMS-1500.html", "text/html", ct);

        var transformJson = BuildTransformJson(boxesDoc.Content);
        var transformDocId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            transformJson, "cms1500Transform", "CMS-1500.transform.json", "application/json", ct);

        logger.LogInformation("HtmlRender complete. boxesDocId={B} → htmlDocId={D}, transformDocId={T}",
            inputDocIds[0], htmlDocId, transformDocId);
        return [htmlDocId, transformDocId];
    }

    private static string BuildTransformJson(string content)
    {
        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(content);
            return node?.ToJsonString(WriteIndented) ?? JsonSerializer.Serialize(new { content });
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { content });
        }
    }
}
