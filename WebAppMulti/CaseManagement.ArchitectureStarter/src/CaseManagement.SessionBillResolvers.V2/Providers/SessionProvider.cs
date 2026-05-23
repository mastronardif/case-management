using System.Data;
using CaseManagement.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class SessionProvider : ISessionProvider
{
    private readonly ConnectionSettings _conn;
    private readonly BillingRunOptions _options;
    private readonly ILogger<SessionProvider> _logger;

    public SessionProvider(ConnectionSettings conn, BillingRunOptions options, ILogger<SessionProvider> logger)
    {
        _conn = conn;
        _options = options;
        _logger = logger;
    }

    public async Task<IEnumerable<SessionData>> GetUnbilledSessionsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Fetching unbilled sessions. CaseNumber: {CaseNumber}, SessionNumber: {SessionNumber}",
            _options.CaseNumber ?? "all", _options.SessionNumber?.ToString() ?? "all");

        var parameters = new DynamicParameters();
        parameters.Add("@CaseNumber", _options.CaseNumber);
        parameters.Add("@SessionNumber", _options.SessionNumber);

        await using var conn = new SqlConnection(_conn.DefaultConnection);
        return await conn.QueryAsync<SessionData>("usp_GetUnbilledSessions", parameters, commandType: CommandType.StoredProcedure);
    }
}