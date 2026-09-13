using System.Text.Json;
using CaseManagement.SessionBillResolvers.V2.Engine;
using CaseManagement.SessionBillResolvers.V2.Engine.Dsl;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Jobs;

// In-process replacement for BuildQueueForClearingHouse.ps1's Invoke-PipelineStep (which
// spawned `dotnet run -- --expression ...` as a subprocess and regex-scraped the doc id back
// out of its console text) and its Invoke-RestMethod calls against /api/saveWorkflow and
// /api/getDocument. Same engine, same repository, called as real method calls — no subprocess,
// no text parsing, no HTTP round trip to a server that has to be running.
public class DslRunner(ICaseManagementRepository repo, WorkflowEngine engine, ILogger<DslRunner> logger)
{
    private OperatorRegistry? _registry;

    private async Task<OperatorRegistry> GetRegistryAsync(CancellationToken ct)
    {
        if (_registry is not null) return _registry;

        var operatorsDocId = await repo.GetConstantAsync("Operators", ct)
            ?? throw new InvalidOperationException("MyConstants key 'Operators' not found.");
        var operatorsDoc = await repo.GetDocumentAsync(new DocumentContext(DocumentId: operatorsDocId), ct)
            ?? throw new InvalidOperationException($"Operators registry doc {operatorsDocId} not found.");

        _registry = OperatorRegistry.FromJson(operatorsDoc.Content).MergeWith(OperatorRegistry.BuiltInTokens);
        return _registry;
    }

    // Runs a chained-operator expression (e.g. "123 (P) 456 (AM) 789") and returns every
    // output doc id produced, in order — same shape as the PS1 script's Invoke-PipelineStep,
    // just returned as real int[] instead of parsed console text.
    public async Task<int[]> RunAsync(string expression, int caseId, CancellationToken ct)
    {
        logger.LogInformation("DSL: {Expression}", expression);

        var registry = await GetRegistryAsync(ct);
        var ast = PipelineParser.Parse(expression, registry);
        var workflow = PipelineCompiler.Compile(ast);

        var overrides = new Dictionary<string, JsonElement> { ["caseId"] = JsonSerializer.SerializeToElement(caseId) };
        var outputs = await engine.RunAsync(workflow, overrides, ct);
        var flat = outputs.SelectMany(step => step).ToArray();

        logger.LogInformation("  => {DocIds}", string.Join(", ", flat));
        return flat;
    }

    // Convenience for the common case: run an expression and take its last output doc id
    // (every (P)(AM)/(Q)/(W)/(Y) step in this job only ever needs the final result).
    public async Task<int> RunSingleAsync(string expression, int caseId, CancellationToken ct)
    {
        var outputs = await RunAsync(expression, caseId, ct);
        if (outputs.Length == 0) throw new InvalidOperationException($"No output docId produced by expression: {expression}");
        return outputs[^1];
    }

    // Mirrors /api/saveWorkflow's exact behavior (DocumentType="workflow", Title=name,
    // FileName="{name}.json") so snapshot docs stay identical to what that endpoint produces.
    public async Task<int> SaveSnapshotAsync(string json, string name, CancellationToken ct)
    {
        var docId = await repo.SaveDocumentAsync(
            new DocumentContext(), json, documentType: "workflow", fileName: $"{name}.json", contentType: "application/json", ct);
        logger.LogInformation("Saved snapshot '{Name}': docId {DocId}", name, docId);
        return docId;
    }

    public async Task<Document> GetDocumentAsync(int docId, CancellationToken ct) =>
        await repo.GetDocumentAsync(new DocumentContext(DocumentId: docId), ct)
            ?? throw new InvalidOperationException($"Document {docId} not found.");
}
