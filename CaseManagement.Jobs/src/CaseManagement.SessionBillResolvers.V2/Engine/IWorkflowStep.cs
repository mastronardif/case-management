namespace CaseManagement.SessionBillResolvers.V2.Engine;

public interface IWorkflowStep
{
    string Operator { get; }
    Task<int[]> ExecuteAsync(int[] inputDocIds, string runId, IReadOnlyDictionary<string, System.Text.Json.JsonElement>? wfParams, CancellationToken ct);
}
