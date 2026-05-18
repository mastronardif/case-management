using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;
using System.Text;

namespace CaseManagement.DocumentResolvers.Resolvers;

public class PayerCsvResolver
{
    private readonly IConfiguration _config;

    public PayerCsvResolver(IConfiguration config)
    {
        _config = config;
    }

    public async Task RunAsync(int documentId)
    {
        var connectionString =
            _config.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "[cases].[usp_Document_GetById]",
            conn);

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("@DocumentId", SqlDbType.Int).Value = documentId;

        byte[] fileData;

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
                return;

            fileData = (byte[])reader["FileData"];
        }

        await ProcessCsvAsync(conn, fileData);
    }

    private async Task ProcessCsvAsync(SqlConnection conn, byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var streamReader = new StreamReader(stream, Encoding.UTF8);

        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        var rows = csv.GetRecords<PayerCsvRow>();

        foreach (var row in rows)
        {
            using var cmd = new SqlCommand(
                "[cases].[usp_Payer_Upsert]",
                conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@PublicId", SqlDbType.UniqueIdentifier)
                .Value = DBNull.Value; // let SQL generate it

            cmd.Parameters.Add("@PayerName", SqlDbType.NVarChar, 200)
                .Value = row.PayerName ?? throw new Exception("PayerName required");

            cmd.Parameters.Add("@PayerCode", SqlDbType.NVarChar, 50)
                .Value = DbValue(row.PayerCode);

            cmd.Parameters.Add("@StateCode", SqlDbType.NVarChar, 10)
                .Value = DbValue(row.StateCode);

            cmd.Parameters.Add("@Website", SqlDbType.NVarChar, 500)
                .Value = DbValue(row.Website);

            cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 50)
                .Value = DbValue(row.PhoneNumber);

            cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 100)
                .Value = "DocumentResolver";

            cmd.Parameters.Add("@IsActive", SqlDbType.Bit)
                .Value = 1;

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private object DbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DBNull.Value
            : value.Trim();
    }
}

public class PayerCsvRow
{
    public string? PayerName { get; set; }
    public string? PayerCode { get; set; }
    public string? StateCode { get; set; }
    public string? Website { get; set; }
    public string? PhoneNumber { get; set; }
}