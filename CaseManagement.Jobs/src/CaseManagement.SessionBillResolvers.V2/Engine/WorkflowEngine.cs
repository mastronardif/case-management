using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public class WorkflowEngine(
    IEnumerable<IWorkflowStep> steps,
    ICaseManagementRepository repository,
    ILogger<WorkflowEngine> logger)
{
    private static readonly JsonSerializerOptions JsonOptions     = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions ManifestOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly Dictionary<string, IWorkflowStep> _steps = steps.ToDictionary(s => s.Operator);

    public async Task<int[][]> RunAsync(int workflowDocId, CancellationToken ct)
        => await RunAsync(workflowDocId, null, ct);

    public async Task<int[][]> RunAsync(int workflowDocId, IReadOnlyDictionary<string, JsonElement>? paramOverrides, CancellationToken ct)
    {
        var runId     = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        var workflowDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: workflowDocId), ct)
            ?? throw new InvalidOperationException($"Workflow doc {workflowDocId} not found");

        var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(workflowDoc.Content, JsonOptions)
            ?? throw new InvalidOperationException($"Doc {workflowDocId} could not be deserialized as a WorkflowDefinition");

        if (workflow.Steps is null || workflow.Steps.Count == 0)
            throw new InvalidOperationException($"Doc {workflowDocId} (type: {workflowDoc.DocumentType}) is not a workflow — no steps found. Import the workflow JSON first to get its DocumentId.");

        var mergedParams = new Dictionary<string, JsonElement>(workflow.Params ?? new());
        if (paramOverrides is not null)
            foreach (var (key, value) in paramOverrides)
                mergedParams[key] = value;

        logger.LogInformation("Workflow {WorkflowId} v{Version} started. RunId: {RunId}, Steps: {StepCount}",
            workflow.WorkflowId, workflow.Version, runId, workflow.Steps.Count);

        var stepOutputs = new int[workflow.Steps.Count][];
        var stepResults = new List<WorkflowStepResult>();

        for (int i = 0; i < workflow.Steps.Count; i++)
        {
            var step = workflow.Steps[i];

            if (!_steps.TryGetValue(step.Operator, out var handler))
                throw new InvalidOperationException($"No handler registered for operator '{step.Operator}'");

            var inputDocIds = step.Input
                .Select(token => ResolveInput(token, stepOutputs, i, mergedParams))
                .ToArray();


            logger.LogInformation("Step [{Index}] {Id} ({Operator}) inputs: [{Inputs}]",
                i + 1, step.Id, step.Operator, string.Join(", ", inputDocIds));

            stepOutputs[i] = await handler.ExecuteAsync(inputDocIds, runId, mergedParams, ct);

            logger.LogInformation("Step [{Index}] {Id} complete. Outputs: [{DocIds}]",
                i + 1, step.Id, string.Join(", ", stepOutputs[i]));

            stepResults.Add(new WorkflowStepResult(
                Id:              step.Id,
                Operator:        step.Operator,
                InputTokens:     step.Input.Select(TokenString).ToArray(),
                OutputNames:     step.Output,
                ResolvedInputs:  inputDocIds,
                ResolvedOutputs: stepOutputs[i]));
        }

        var completedAt = DateTime.UtcNow;

        logger.LogInformation("Workflow {WorkflowId} complete. RunId: {RunId}, Outputs: [{Outputs}]",
            workflow.WorkflowId, runId, string.Join(" | ", stepOutputs.Select(s => string.Join(", ", s))));

        var manifest = new WorkflowRunManifest(
            RunId:        runId,
            WorkflowDocId: workflowDocId,
            WorkflowId:   workflow.WorkflowId,
            Version:      workflow.Version,
            StartedAt:    startedAt,
            CompletedAt:  completedAt,
            Steps:        stepResults);

        var manifestJson = JsonSerializer.Serialize(manifest, ManifestOptions);
        var manifestDocId = await repository.SaveDocumentAsync(
            new DocumentContext(),
            manifestJson,
            "workflowRun",
            $"{runId}.workflowRun.json",
            "application/json",
            ct);

        logger.LogInformation("Run manifest saved. DocId: {DocId}, RunId: {RunId}", manifestDocId, runId);

        return stepOutputs;
    }

    // Integer JSON element → direct docId. String → D1 step-ref, $paramName lookup, or literal int.
    private static int ResolveInput(System.Text.Json.JsonElement token, int[][] stepOutputs, int currentStepIndex,
        IReadOnlyDictionary<string, System.Text.Json.JsonElement> mergedParams)
    {
        if (token.ValueKind == System.Text.Json.JsonValueKind.Number)
            return token.GetInt32();

        var key = (token.GetString() ?? "").Split(' ')[0];

        if (key.StartsWith("$"))
        {
            var paramName = key[1..];
            if (!mergedParams.TryGetValue(paramName, out var paramValue))
                throw new InvalidOperationException($"Workflow param '{paramName}' (referenced as '{key}') was not provided");
            return paramValue.GetInt32();
        }

        if (key.StartsWith("D", StringComparison.OrdinalIgnoreCase))
        {
            var stepIndex = int.Parse(key[1..]) - 1;
            if (stepIndex >= currentStepIndex)
                throw new InvalidOperationException($"Step reference '{key}' refers to a step that hasn't completed yet");
            return stepOutputs[stepIndex][0];
        }

        return int.Parse(key);
    }

    private static string TokenString(System.Text.Json.JsonElement t) =>
        t.ValueKind == System.Text.Json.JsonValueKind.Number ? t.GetInt32().ToString() : (t.GetString() ?? "");
}
