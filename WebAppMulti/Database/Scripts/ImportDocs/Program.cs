using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.MSSqlServer;
using Serilog;

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
            
        //Log.Information("TEST DB LOG");
        //Log.Error("TEST ERROR DB LOG");

        try
        {
            Log.Information("Starting ImportDocs");

            if (!File.Exists(csvPath))
            {
                Log.Error("CSV file not found: {CsvPath}", csvPath);
                return 1;
            }

            var connectionString = config.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var lines = File.ReadAllLines(csvPath);

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');

                if (parts.Length < 5)
                {
                    Log.Warning("Skipping invalid row: {Row}", lines[i]);
                    continue;
                }

                string caseNumber = parts[0];
                //object caseNumberValue = string.IsNullOrWhiteSpace(caseNumber) ? DBNull.Value : caseNumber.Trim();
        
                string filePath = parts[1];
                string documentType = parts[2];
                string title = parts[3];
                string contentType = parts[4];

                if (!File.Exists(filePath))
                {
                    Log.Warning("File not found: {FilePath}", filePath);
                    continue;
                }

                byte[] fileBytes;

                try
                {
                    fileBytes = File.ReadAllBytes(filePath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed reading file: {FilePath}", filePath);
                    continue;
                }

                using var transaction = conn.BeginTransaction();

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

                    cmd.Parameters.Add("@VersionId",    SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                    //cmd.Parameters.Add("@CaseNumber",   SqlDbType.NVarChar, 50).Value = caseNumber;
                    cmd.Parameters.Add("@CaseNumber",   SqlDbType.NVarChar, 50).Value = DbValue(caseNumber);
                    
                    cmd.Parameters.Add("@DocumentType", SqlDbType.VarChar, 50).Value = documentType;
                    cmd.Parameters.Add("@Title",        SqlDbType.NVarChar, 200).Value = title;
                    cmd.Parameters.Add("@FileName",     SqlDbType.NVarChar, 255).Value = Path.GetFileName(filePath);
                    cmd.Parameters.Add("@ContentType",  SqlDbType.VarChar, 100).Value = contentType;
                    cmd.Parameters.Add("@FileData",     SqlDbType.VarBinary, -1).Value = fileBytes;
                    cmd.Parameters.Add("@CreatedBy",    SqlDbType.NVarChar, 100).Value = "ImportDocs";

                    cmd.ExecuteNonQuery();

                    transaction.Commit();

                    Log.Information("Imported {File} for CaseNumber {CaseNumber}", filePath, caseNumber);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Transaction failed for file: {FilePath}", filePath);
                }
            }

            Log.Information("Import completed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in ImportDocs");
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
}
//dotnet run -- import.csv "Server=.;Database=YourDb;Trusted_Connection=True;TrustServerCertificate=True;"
// dotnet run -- import.csv "Server=LAPTOP-JIH94VS9\SQLEXPRESS;Database=AdventureWorksDW;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"


