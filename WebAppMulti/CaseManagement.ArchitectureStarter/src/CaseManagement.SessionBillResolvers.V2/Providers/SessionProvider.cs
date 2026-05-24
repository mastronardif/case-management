using System.Data;
using CaseManagement.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        if (11 == 11)
        {
            var mockSessions = new List<SessionData>
            {
                new SessionData { SessionId = 1, CaseNumber = "CASE123", DurationMinutes = 60 },
                new SessionData { SessionId = 2, CaseNumber = "CASE456", DurationMinutes = 30 }
            };
            if (_options.Mode == BillingRunMode.SingleRun)
            {
                mockSessions.RemoveAt(1); // Return only one session for single run mode
            }

            return mockSessions; // TODO: replace with real data access
        }
        else
        {



            var parameters = new DynamicParameters();
            parameters.Add("@CaseNumber", _options.CaseNumber);
            parameters.Add("@SessionNumber", _options.SessionNumber);

            await using var conn = new SqlConnection(_conn.DefaultConnection);
            return await conn.QueryAsync<SessionData>("usp_GetUnbilledSessions", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}