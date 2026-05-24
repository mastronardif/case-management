using System.Data;
using CaseManagement.Shared;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CaseManagement.SessionBillResolvers.V2;

public class SqlBillingRepository : IBillingRepository
{
    private readonly ConnectionSettings _conn;

    public SqlBillingRepository(ConnectionSettings conn)
    {
        _conn = conn;
    }

    public async Task SaveInvoiceAsync(Invoice invoice, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_conn.DefaultConnection);
        Console.WriteLine($"Saving invoice for case {invoice.SessionId}, amount {invoice.Amount}");
        //await conn.ExecuteAsync("usp_SaveBillingInvoice", invoice, commandType: CommandType.StoredProcedure);
        Console.WriteLine($"await conn.ExecuteAsync(\"usp_SaveBillingInvoice\", invoice, commandType: CommandType.StoredProcedure);");
    }
}
