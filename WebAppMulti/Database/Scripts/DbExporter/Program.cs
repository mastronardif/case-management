using Microsoft.Data.SqlClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var connectionString =
            "Server=LAPTOP-JIH94VS9\\SQLEXPRESS;Database=AdventureWorksDW;Trusted_Connection=True;Encrypt=False;";

        var basePath =
            @"C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti\Database\Artifacts";

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var exporter = new DatabaseExportEngine(conn, basePath);

        await exporter.ExportAllAsync();

        Console.WriteLine("DONE");
    }
}
