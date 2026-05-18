using CaseManagement.DocumentResolvers.Resolvers;
using Microsoft.Extensions.Configuration;
using Serilog;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

try
{

    if (args.Length < 2)
    {
        Console.WriteLine(
            "Usage: CaseManagement.DocumentResolvers <DocumentId> <Type>");

        Console.WriteLine(
            "Example: CaseManagement.DocumentResolvers 42 Payer");

        return;
    }

    if (!int.TryParse(args[0], out var documentId))
    {
        Console.WriteLine("Invalid DocumentId");
        return;
    }

    var type = args[1];

    Console.WriteLine($"DocumentId: {documentId}");
    Console.WriteLine($"Type: {type}");

    Log.Information("Starting Document Resolver");

    var resolver = new PayerCsvResolver(config);

    await resolver.RunAsync(documentId);

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
