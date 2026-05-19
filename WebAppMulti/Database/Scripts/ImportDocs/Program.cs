using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.MSSqlServer;
using Serilog;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

public class DocumentImportRow
{
    public string? CaseNumber { get; set; }
    public string? FilePath { get; set; }
    public string? DocumentType { get; set; }
    public string? Title { get; set; }
    public string? ContentType { get; set; }
}
class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ImportDocs <csvPath>");
            return 1;
        }

        var csvPath = args[0];

        // Load shared config (same as main app)
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();
            
        return ImportDocuments(csvPath, config);
        //Log.Information("TEST DB LOG");
        //Log.Error("TEST ERROR DB LOG");

    }
    
    static int ImportDocuments(    string csvPath,    IConfiguration config)
    {
        try
        {
        Log.Information("Starting ImportDocs");

        if (!File.Exists(csvPath))
        {
            Log.Error("CSV file not found: {CsvPath}", csvPath);
            return 1;
        }

        var connectionString =
            config.GetConnectionString("DefaultConnection");

        using var conn = new SqlConnection(connectionString);

        conn.Open();

        var rows = ReadImportCsv(csvPath);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.FilePath))
            {
                Log.Warning(
                    "Skipping row with empty FilePath"
                );

                continue;
            }

            if (!File.Exists(row.FilePath))
            {
                Log.Warning(
                    "File not found: {FilePath}",
                    row.FilePath
                );

                continue;
            }

            byte[] fileBytes;

            try
            {
                fileBytes = File.ReadAllBytes(row.FilePath);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed reading file: {FilePath}",
                    row.FilePath
                );

                continue;
            }

            using var transaction =
                conn.BeginTransaction();

            try
            {
                using var cmd = new SqlCommand(@"
INSERT INTO [cases].[Document]
(
    VersionId,
    CaseNumber,
    DocumentType,
    Title,
    FileName,
    ContentType,
    FileData,
    CreatedDate,
    CreatedBy,
    IsActive
)
VALUES
(
    @VersionId,
    @CaseNumber,
    @DocumentType,
    @Title,
    @FileName,
    @ContentType,
    @FileData,
    SYSUTCDATETIME(),
    @CreatedBy,
    1
)", conn, transaction);

                cmd.Parameters
                    .Add(
                        "@VersionId",
                        SqlDbType.UniqueIdentifier
                    )
                    .Value = Guid.NewGuid();

                cmd.Parameters
                    .Add(
                        "@CaseNumber",
                        SqlDbType.NVarChar,
                        50
                    )
                    .Value = DbValue(row.CaseNumber);

                cmd.Parameters
                    .Add(
                        "@DocumentType",
                        SqlDbType.VarChar,
                        50
                    )
                    .Value = DbValue(row.DocumentType);

                cmd.Parameters
                    .Add(
                        "@Title",
                        SqlDbType.NVarChar,
                        200
                    )
                    .Value = DbValue(row.Title);

                cmd.Parameters
                    .Add(
                        "@FileName",
                        SqlDbType.NVarChar,
                        255
                    )
                    .Value = Path.GetFileName(row.FilePath);

                cmd.Parameters
                    .Add(
                        "@ContentType",
                        SqlDbType.VarChar,
                        100
                    )
                    .Value = DbValue(row.ContentType);

                cmd.Parameters
                    .Add(
                        "@FileData",
                        SqlDbType.VarBinary,
                        -1
                    )
                    .Value = fileBytes;

                cmd.Parameters
                    .Add(
                        "@CreatedBy",
                        SqlDbType.NVarChar,
                        100
                    )
                    .Value = "ImportDocs";

                cmd.ExecuteNonQuery();

                transaction.Commit();

                Log.Information(
                    "Imported {File} for CaseNumber {CaseNumber}",
                    row.FilePath,
                    row.CaseNumber
                );
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Log.Error(
                    ex,
                    "Transaction failed for file: {FilePath}",
                    row.FilePath
                );
            }
        }

        Log.Information(
            "Import completed successfully."
        );

        return 0;
    }
    catch (Exception ex)
    {
        Log.Fatal(
            ex,
            "Fatal error in ImportDocs"
        );

        return 1;
    }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    
    static object DbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
    
    static IEnumerable<DocumentImportRow> ReadImportCsv(string csvPath)
    {
        using var reader = new StreamReader(csvPath);

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, csvConfig);

        var records = csv.GetRecords<dynamic>();

        foreach (var record in records)
        {
            var row = (IDictionary<string, object>)record;

            yield return new DocumentImportRow
            {
                CaseNumber = GetValue(row, "CaseNumber"),
            FilePath = GetValue(row, "FilePath"),
            DocumentType = GetValue(row, "DocumentType"),
            Title = GetValue(row, "Title"),
            ContentType = GetValue(row, "ContentType")
            };
        }
    }

    static string GetValue(IDictionary<string, object> row, string columnName)
    {
        if (row.TryGetValue(columnName, out var value))
        {
            return value?.ToString()?.Trim() ?? "";
        }

        return "";
    }
}
//dotnet run -- import.csv "Server=.;Database=YourDb;Trusted_Connection=True;TrustServerCertificate=True;"
// dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\SQLEXPRESS;Database=CaseManagement;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"


