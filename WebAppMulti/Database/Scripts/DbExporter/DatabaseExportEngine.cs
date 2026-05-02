using Microsoft.Data.SqlClient;

internal class DatabaseExportEngine
{
    private readonly SqlConnection _conn;
    private readonly string _basePath;

    public DatabaseExportEngine(SqlConnection conn, string basePath)
    {
        _conn = conn;
        _basePath = basePath;
    }

    public async Task ExportAllAsync()
    {
        var exporters = new List<(string Folder, IExporter Exporter)>
        {
            ("Tables", new TableExporter()),
            ("Views", new ViewExporter()),
            ("StoredProcedures", new StoredProcedureExporter()),
            ("Functions", new FunctionExporter())
        };

        foreach (var (folder, exporter) in exporters)
        {
            var path = Path.Combine(_basePath, folder);
            Directory.CreateDirectory(path);

            Console.WriteLine($"Exporting {folder}...");

            await exporter.ExportAsync(_conn, path);
        }

        Console.WriteLine("Building deploy script...");
        await DeployScriptBuilder.BuildAsync(_basePath);
    }
}
