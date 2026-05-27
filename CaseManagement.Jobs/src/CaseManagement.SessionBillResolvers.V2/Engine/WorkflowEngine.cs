using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public class WorkflowEngine(
    IEnumerable<IWorkflowStep> steps,
    ICaseManagementRepository repository,
    ILogger<WorkflowEngine> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, IWorkflowStep> _steps = steps.ToDictionary(s => s.StepType);

    public async Task<int[]> RunAsync(int workflowDocId, CancellationToken ct)
    {
        var workflowDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: workflowDocId), ct)
            ?? throw new InvalidOperationException($"Workflow doc {workflowDocId} not found");

        var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(workflowDoc.Content, JsonOptions)
            ?? throw new InvalidOperationException($"Doc {workflowDocId} could not be deserialized as a WorkflowDefinition");

        if (workflow.Steps is null || workflow.Steps.Count == 0)
            throw new InvalidOperationException($"Doc {workflowDocId} (type: {workflowDoc.DocumentType}) is not a workflow — no steps found. Import the workflow JSON first to get its DocumentId.");

        logger.LogInformation("Workflow {WorkflowId} v{Version} started. Steps: {StepCount}",
            workflow.WorkflowId, workflow.Version, workflow.Steps.Count);

        var stepOutputs = new int[workflow.Steps.Count];

        for (int i = 0; i < workflow.Steps.Count; i++)
        {
            var step = workflow.Steps[i];

            if (!_steps.TryGetValue(step.Type, out var handler))
                throw new InvalidOperationException($"No handler registered for step type '{step.Type}'");

            var inputDocIds = step.Input
                .Select(token => ResolveInput(token, stepOutputs, i))
                .ToArray();

            logger.LogInformation("Step [{Index}] {Id} ({Type}) inputs: [{Inputs}]",
                i + 1, step.Id, step.Type, string.Join(", ", inputDocIds));

            stepOutputs[i] = await handler.ExecuteAsync(inputDocIds, ct);

            logger.LogInformation("Step [{Index}] {Id} complete. OutputDocId: {DocId}",
                i + 1, step.Id, stepOutputs[i]);
        }

        logger.LogInformation("Workflow {WorkflowId} complete. Outputs: [{Outputs}]",
            workflow.WorkflowId, string.Join(", ", stepOutputs));

        return stepOutputs;
    }

    private static int ResolveInput(string token, int[] stepOutputs, int currentStepIndex)
    {
        var key = token.Split(' ')[0];

        if (key.StartsWith("D", StringComparison.OrdinalIgnoreCase))
        {
            var stepIndex = int.Parse(key[1..]) - 1;
            if (stepIndex >= currentStepIndex)
                throw new InvalidOperationException($"Step reference '{key}' refers to a step that hasn't completed yet");
            return stepOutputs[stepIndex];
        }

        return int.Parse(key);
    }
}
