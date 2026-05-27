namespace CaseManagement.SessionBillResolvers.V2.Engine;

public interface IWorkflowStep
{
    string StepType { get; }
    Task<int> ExecuteAsync(int[] inputDocIds, CancellationToken ct);
}
