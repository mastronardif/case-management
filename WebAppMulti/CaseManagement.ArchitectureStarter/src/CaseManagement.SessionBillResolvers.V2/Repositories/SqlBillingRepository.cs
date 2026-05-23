using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CaseManagement.SessionBillResolvers.V2;

public class SqlBillingRepository : IBillingRepository
{
    private readonly string _connectionString;

    public SqlBillingRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveInvoiceAsync(Invoice invoice, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync("usp_SaveBillingInvoice", invoice, commandType: CommandType.StoredProcedure);
    }
}