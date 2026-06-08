using Microsoft.Data.SqlClient;
using System.Data;

public static class SaveWorkflowEndpoint
{
    public static void MapSaveWorkflowEndpoint(this WebApplication app)
    {
        app.MapPost("/api/saveWorkflow", async (SaveWorkflowRequest req, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(req.Json))
                return Results.BadRequest("json is required");

            var name     = string.IsNullOrWhiteSpace(req.Name) ? "workflow" : req.Name;
            var fileName = $"{name}.json";
            var data     = System.Text.Encoding.UTF8.GetBytes(req.Json);

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("[cases].[usp_Document_Save]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CaseId",       DBNull.Value);
            cmd.Parameters.AddWithValue("@SessionId",    DBNull.Value);
            cmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
            cmd.Parameters.AddWithValue("@CaseNumber",   DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentType", "workflow");
            cmd.Parameters.AddWithValue("@Title",        name);
            cmd.Parameters.AddWithValue("@FileName",     fileName);
            cmd.Parameters.AddWithValue("@ContentType",  "application/json");
            cmd.Parameters.AddWithValue("@FileData",     data);
            cmd.Parameters.AddWithValue("@CreatedBy",    "pipeline-catalog");

            var docIdParam = new SqlParameter("@DocumentId", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value     = DBNull.Value
            };
            cmd.Parameters.Add(docIdParam);

            await cmd.ExecuteNonQueryAsync();
            var docId = (int)docIdParam.Value;

            return Results.Ok(new { docId, name, fileName });
        });
    }
}

public record SaveWorkflowRequest(string? Json, string? Name);
