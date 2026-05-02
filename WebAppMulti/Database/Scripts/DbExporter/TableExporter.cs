using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Text;

internal class TableExporter : IExporter
{
    public Task ExportAsync(SqlConnection conn, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        var serverConnection = new ServerConnection(conn);
        var server = new Server(serverConnection);

        var database = server.Databases[conn.Database]
            ?? throw new Exception("Database not found.");

        var scripter = new Scripter(server)
        {
            Options =
            {
                ScriptDrops = false,
                WithDependencies = false,
                SchemaQualify = true,
                DriAll = true,
                Indexes = true,
                Triggers = true
            }
        };

        var tables = database.Tables
            .Cast<Table>()
            .Where(t => !t.IsSystemObject)
            .OrderBy(t => t.Schema)
            .ThenBy(t => t.Name);

        foreach (var table in tables)
        {
            var script = scripter.Script(new[] { table });

            var sb = new StringBuilder();

            sb.AppendLine($"-- TABLE: {table.Schema}.{table.Name}");

            foreach (var line in script)
                sb.AppendLine(line);

            var filePath = Path.Combine(outputPath, $"{table.Schema}.{table.Name}.sql");

            File.WriteAllText(filePath, sb.ToString());
        }

        return Task.CompletedTask;
    }
}
