using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class SessionProvider : ISessionProvider
{
    private readonly string _connectionString;
    private readonly ILogger<SessionProvider> _logger;

    public SessionProvider(string connectionString, ILogger<SessionProvider> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IEnumerable<SessionData>> GetUnbilledSessionsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Fetching unbilled sessions");

        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<SessionData>("usp_GetUnbilledSessions", commandType: CommandType.StoredProcedure);
        return results;
    }
}