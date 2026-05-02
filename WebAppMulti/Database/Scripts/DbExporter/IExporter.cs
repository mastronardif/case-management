using Microsoft.Data.SqlClient;

internal interface IExporter
{
    Task ExportAsync(SqlConnection conn, string outputPath);
}
