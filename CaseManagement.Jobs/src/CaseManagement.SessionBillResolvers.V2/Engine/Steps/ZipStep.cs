using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

public class ZipStep(ICaseManagementRepository repository, ILogger<ZipStep> logger) : IWorkflowStep
{
    public string Operator => "zip";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new(
        Operator:       "zip",
        Description:    "Packages any number of input documents into a single ZIP archive.",
        InputLabels:    ["doc1 … docN (variable)"],
        OutputLabels:   ["report.zip"],
        Params:
        [
            new("caseId", "int", false, "CaseId for document context")
        ],
        AlgebraExample: "(Z)  →  (Z)[A,B,C]  — bundle A, B, C into archive",
        CliExample:     "--expression '(Z)[$jsonDocId, $ruleDocId, 206]' --json-doc-id 403 --rule-doc-id 514",
        Token:          "Z");

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId, IReadOnlyDictionary<string, System.Text.Json.JsonElement>? wfParams, CancellationToken ct)
    {
        int? caseId = wfParams != null && wfParams.TryGetValue("caseId", out var v) ? v.GetInt32() : null;
        using var ms = new MemoryStream();

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in inputDocIds)
            {
                var doc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: id), ct)
                    ?? throw new InvalidOperationException($"Document {id} not found");

                //var entryName = doc.FileName ?? $"doc-{id}.bin";
                var entryName = $"{id}." + (doc.FileName ?? $"doc-{id}.bin");

                var entry     = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(doc.FileData, ct);
            }
        }

        var docId = await repository.SaveDocumentAsync(
            new DocumentContext(CaseId: caseId),
            ms.ToArray(),
            "reportZip",
            $"{runId}.report.zip",
            "application/zip",
            ct);

        logger.LogInformation("Zip complete. {Count} docs → docId {DocId}", inputDocIds.Length, docId);
        return [docId];
    }
}
