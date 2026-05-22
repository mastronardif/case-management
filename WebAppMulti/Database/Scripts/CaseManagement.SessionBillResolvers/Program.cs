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
    if (args.Length == 0 ||
        args.Contains("--help", StringComparer.OrdinalIgnoreCase) ||
        args.Contains("-h", StringComparer.OrdinalIgnoreCase))
    {
        PrintHelp();
        return;
    }

    if (args.Length < 2)
    {
        Console.WriteLine("Missing required arguments.");
        Console.WriteLine();

        PrintHelp();

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

    Log.Information("Starting SessionBillResolvers");

    // TODO:
    // var resolver = ...
    // await resolver.RunAsync(documentId);

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

static void PrintHelp()
{
    Console.WriteLine("CaseManagement.SessionBillResolvers");
    Console.WriteLine();

    Console.WriteLine("Creates billing artifacts from session-related documents.");
    Console.WriteLine();

    Console.WriteLine("Usage:");
    Console.WriteLine("  CaseManagement.SessionBillResolvers <DocumentId> <Type>");
    Console.WriteLine();

    Console.WriteLine("Arguments:");
    Console.WriteLine("  arg1  DocumentId");
    Console.WriteLine("        Source document identifier.");
    Console.WriteLine();

    Console.WriteLine("  arg2  Type");
    Console.WriteLine("        Billing rule / resolver type.");
    Console.WriteLine("        Example: SessionNote");
    Console.WriteLine();

    Console.WriteLine("Examples:");
    Console.WriteLine("  CaseManagement.SessionBillResolvers 24 SessionNote");
    Console.WriteLine();

    Console.WriteLine("Options:");
    Console.WriteLine("  --help, -h     Display help");
}