using Microsoft.Data.SqlClient;
using System.Data;

public static class GetDocumentEndpoint
{
    public static void MapGetDocumentEndpoint(this WebApplication app)
    {
        app.MapGet("/api/getDocument", async (
            HttpRequest request,
            IConfiguration config) =>
        {
            var q = request.Query;
            var documentId   = q["docId"].FirstOrDefault();
            var caseId       = q["caseId"].FirstOrDefault();
            var workbookQId  = q["workbookQId"].FirstOrDefault();
            var sessionId    = q["sessionId"].FirstOrDefault();
            var documentType = q["documentType"].FirstOrDefault();

            var connStr = config.GetConnectionString("DefaultConnection");
            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand("cases.usp_Document_GetByContext", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@DocumentId",   (object?)ParseInt(documentId)  ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CaseId",       (object?)caseId                ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WorkbookQId",  (object?)ParseInt(workbookQId) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SessionId",    (object?)ParseInt(sessionId)   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentType", (object?)documentType          ?? DBNull.Value);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            if (!await reader.ReadAsync())
                return Results.NotFound("No document found for the given context.");

            var returnedDocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId"));
            var fileName           = reader.IsDBNull(reader.GetOrdinal("FileName")) ? null : reader.GetString(reader.GetOrdinal("FileName"));
            var contentType        = reader.GetString(reader.GetOrdinal("ContentType"));
            var fileData           = (byte[])reader.GetValue(reader.GetOrdinal("FileData"));

            request.HttpContext.Response.Headers["X-Document-Id"] = returnedDocumentId.ToString();

            if (contentType == "application/zip")
            {
                var download = fileName ?? $"doc-{returnedDocumentId}.zip";
                return Results.File(fileData, contentType, fileDownloadName: download);
            }

            return Results.File(fileData, contentType);
        });
    }

    private static int? ParseInt(string? val) =>
        int.TryParse(val, out var n) ? n : null;
}
