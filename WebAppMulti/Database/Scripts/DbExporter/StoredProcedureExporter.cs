using Microsoft.Data.SqlClient;
using System.Text;

internal class StoredProcedureExporter : IExporter
{
    public async Task ExportAsync(SqlConnection conn, string outputPath)
    {
        var cmd = new SqlCommand(@"
            SELECT name, OBJECT_DEFINITION(object_id)
            FROM sys.procedures
            WHERE is_ms_shipped = 0
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var definition = reader.IsDBNull(1) ? "" : reader.GetString(1);

            var filePath = Path.Combine(outputPath, $"{name}.sql");

            await File.WriteAllTextAsync(filePath, definition, Encoding.UTF8);
        }
    }
}
