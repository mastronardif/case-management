using CaseManagement.Common;
using CaseManagement.SessionResolvers.Resolvers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;

// dotnet run -- 18


var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

try
{
    if (args.Length < 1)
    {
        Console.WriteLine(
            "Usage: CaseManagement.SessionResolvers <DocumentId>");

        Console.WriteLine(
            "Example: CaseManagement.SessionResolvers 42");

        return;
    }

    if (!int.TryParse(args[0], out var documentId))
    {
        Console.WriteLine("Invalid DocumentId");
        return;
    }

    //var jsonFile = args[1];

    Console.WriteLine($"DocumentId: {documentId}");
    

    Log.Information("Starting Session Import Resolver");

    var filePath = await RunAsync(documentId);
    var jsonFile = Path.Combine(@"C:\temp", Path.GetFileName(filePath) + ".json");

    Console.WriteLine($"\n " +  $"**********\n File ready for AI step: \n{filePath}\n");
    Console.WriteLine($"Pause: AI convert document {filePath} → {filePath}.json JSON manually\n");
    Console.WriteLine($"\n " + $"**********\n");
    Console.ReadLine();    
    Console.WriteLine($"JsonFile: {filePath}.json\n");

    var resolver = new SessionImportResolver(config);

    await resolver.RunAsync(jsonFile, documentId);

    Log.Information("Completed successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal error");
}
finally
{
    Log.CloseAndFlush();
}

async Task<string> RunAsync(int documentId)
{
    var connectionString =
        config.GetConnectionString("DefaultConnection");

    using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var (fileData, fileName) =
        await DocumentFileFetcher.GetDocumentAsync(conn, documentId);

    Log.Information("Document loaded: {FileName}", fileName);

    //var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "staging");
    var outputDir = @"C:\temp";

    if (!Directory.Exists(outputDir))
        Directory.CreateDirectory(outputDir);

    var safeFileName = string.IsNullOrWhiteSpace(fileName)
        ? $"document_{documentId}.bin"
        : fileName;

    safeFileName = MakeSafeFileName(safeFileName);

    var fullPath = Path.Combine(outputDir, safeFileName);

    await File.WriteAllBytesAsync(fullPath, fileData);

    Log.Information("File written to {Path}", fullPath);

    return fullPath;
}

static string MakeSafeFileName(string fileName)
{
    foreach (var c in Path.GetInvalidFileNameChars())
        fileName = fileName.Replace(c, '_');

    return fileName;
}