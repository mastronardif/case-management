namespace CaseManagement.SessionBillResolvers.V2;

public interface ISessionProvider
{
    Task<IEnumerable<SessionData>> GetUnbilledSessionsAsync(CancellationToken ct);
}