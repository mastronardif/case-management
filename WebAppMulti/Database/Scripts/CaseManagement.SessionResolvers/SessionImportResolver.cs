using CaseManagement.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;

namespace CaseManagement.SessionResolvers.Resolvers;

public class SessionImportResolver
{
    private readonly IConfiguration _config;

    public SessionImportResolver(IConfiguration config)
    {
        _config = config;
    }

    public async Task RunAsync(string jsonFile, int documentId)
    {
        var connectionString =
            _config.GetConnectionString("DefaultConnection");

        var json = await File.ReadAllTextAsync(jsonFile);
        var htmlFile = $"{jsonFile}.html";

        var html = JsonToHtmlConverter.Convert(json);        

        await File.WriteAllTextAsync(htmlFile,
            //@"C:\temp\review.html",
            html);

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("[cases].[usp_DocumentExtraction_Insert]", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add("@DocumentId", SqlDbType.Int).Value = documentId;

        cmd.Parameters.Add("@ExtractionType", SqlDbType.NVarChar, 50).Value = "Session";
        //cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar, 100).Value = DBNull.Value;
        cmd.Parameters.Add("@ModelName", SqlDbType.NVarChar, 100).Value = "manual-ai-v1";
        cmd.Parameters.Add("@RawText", SqlDbType.NVarChar).Value = DBNull.Value;

        cmd.Parameters.Add("@ExtractedJson", SqlDbType.NVarChar).Value = json;

        cmd.Parameters.Add("@ValidationErrors", SqlDbType.NVarChar).Value = DBNull.Value;

        cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = "PendingNormalization";

        cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = "SessionResolvers";

        await cmd.ExecuteNonQueryAsync();
    }
    private object DbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DBNull.Value
            : value.Trim();
    }
}