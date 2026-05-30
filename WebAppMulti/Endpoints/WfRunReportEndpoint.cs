using Microsoft.Data.SqlClient;
using System.Data;
using WebAppMulti.Reports;

public static class WfRunReportEndpoint
{
    public static void MapWfRunReportEndpoint(this WebApplication app)
    {
        app.MapGet("/api/wfRunReport", async ([Microsoft.AspNetCore.Mvc.FromQuery] int docId, IConfiguration config) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection");
            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand("cases.usp_Document_GetByContext", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@DocumentId",   docId);
            cmd.Parameters.AddWithValue("@CaseId",       DBNull.Value);
            cmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
            cmd.Parameters.AddWithValue("@SessionId",    DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentType", DBNull.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            if (!await reader.ReadAsync())
                return Results.NotFound($"Document {docId} not found.");

            var fileData     = (byte[])reader.GetValue(reader.GetOrdinal("FileData"));
            var manifestJson = System.Text.Encoding.UTF8.GetString(fileData);
            var html         = WfRunReportRenderer.Render(manifestJson);

            return Results.Content(html, "text/html; charset=utf-8");
        });
    }
}
