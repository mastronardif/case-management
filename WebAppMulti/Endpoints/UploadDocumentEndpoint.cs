using Microsoft.Data.SqlClient;
using System.Data;

public static class UploadDocumentEndpoint
{
    public static void MapUploadDocumentEndpoint(this WebApplication app)
    {
        app.MapPost("/api/uploadDocument", async (HttpRequest request, IConfiguration config) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Request must be multipart/form-data");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file is null || file.Length == 0)
                return Results.BadRequest("No file provided");

            if (!int.TryParse(form["caseId"], out var caseId) || caseId <= 0)
                return Results.BadRequest("caseId is required");

            int? sessionId = int.TryParse(form["sessionId"], out var sid) ? sid : null;
            var documentType = form["documentType"].FirstOrDefault() ?? Path.GetFileNameWithoutExtension(file.FileName);
            var contentType  = file.ContentType.Contains('/') ? file.ContentType : "application/octet-stream";

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileData = ms.ToArray();

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("[cases].[usp_Document_Save]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CaseId",       caseId);
            cmd.Parameters.AddWithValue("@SessionId",    (object?)sessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
            cmd.Parameters.AddWithValue("@CaseNumber",   DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentType", documentType);
            cmd.Parameters.AddWithValue("@Title",        documentType);
            cmd.Parameters.AddWithValue("@FileName",     file.FileName);
            cmd.Parameters.AddWithValue("@ContentType",  contentType);
            cmd.Parameters.AddWithValue("@FileData",     fileData);
            cmd.Parameters.AddWithValue("@CreatedBy",    "upload-ui");

            var docIdParam = new SqlParameter("@DocumentId", SqlDbType.Int)
            {
                Direction = ParameterDirection.InputOutput,
                Value     = DBNull.Value
            };
            cmd.Parameters.Add(docIdParam);

            await cmd.ExecuteNonQueryAsync();
            var docId = (int)docIdParam.Value;

            return Results.Ok(new { docId, fileName = file.FileName, documentType, caseId });
        });
    }
}
