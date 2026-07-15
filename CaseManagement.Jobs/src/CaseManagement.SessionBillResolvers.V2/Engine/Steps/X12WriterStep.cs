using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class X12WriterStep(ICaseManagementRepository repository, ILogger<X12WriterStep> logger) : IWorkflowStep
{
    public string Operator => "x12Writer";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("x12Writer", []);

    public static string BuildOutputFileName(int sourceDocId) => $"{sourceDocId}.edi.txt";

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        var sourceDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: inputDocIds[0]), ct)
            ?? throw new InvalidOperationException($"X12Writer: source doc {inputDocIds[0]} not found");

        int? caseId = wfParams?.TryGetValue("caseId", out var ci) == true && ci.ValueKind == JsonValueKind.Number
            ? ci.GetInt32() : null;

        var writer = new X12Writer();
        var x12Text = writer.Build(sourceDoc.Content);
        var fileName = BuildOutputFileName(inputDocIds[0]);

        var docId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            x12Text, "x12Document", fileName, "text/plain", ct);

        logger.LogInformation("X12Writer complete. sourceDocId={S} → textDocId={D} fileName={FileName}", inputDocIds[0], docId, fileName);
        return [docId];
    }
}
