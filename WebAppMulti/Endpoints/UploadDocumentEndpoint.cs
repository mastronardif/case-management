using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.SqlClient;
using System.Data;

public static class UploadDocumentEndpoint
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    // Browsers send application/octet-stream (or nothing) for any file the OS has no type registered
    // for — PDFs included — and that used to be stored as-is, so the document then downloaded instead of
    // displaying and read as a "binary" to tools that go by type. The client's value is only trusted when
    // it's specific; otherwise the type comes from the file name, with octet-stream as the last resort.
    public static string ResolveContentType(string? clientContentType, string fileName)
    {
        if (!string.IsNullOrEmpty(clientContentType)
            && clientContentType.Contains('/')
            && !clientContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return clientContentType;

        return ContentTypes.TryGetContentType(fileName, out var inferred) ? inferred : "application/octet-stream";
    }

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

            // caseId is optional — cases.Document.CaseId is genuinely nullable (3400+ existing rows
            // already sit at NULL, mostly catalog/projection docs). A document for a brand-new
            // patient is uploaded the same way, with no case yet — Case doesn't get created until
            // a dedicated resolve step confirms it (see usp_NewCase_Assessment_Resolve), never here.
            var caseIdRaw = form["caseId"].FirstOrDefault();
            int? caseId = null;
            if (!string.IsNullOrWhiteSpace(caseIdRaw))
            {
                if (!int.TryParse(caseIdRaw, out var parsedCaseId) || parsedCaseId <= 0)
                    return Results.BadRequest("caseId, if given, must be a positive integer");
                caseId = parsedCaseId;
            }

            int? sessionId = int.TryParse(form["sessionId"], out var sid) ? sid : null;
            var documentType = form["documentType"].FirstOrDefault() ?? Path.GetFileNameWithoutExtension(file.FileName);
            var contentType  = ResolveContentType(file.ContentType, file.FileName);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileData = ms.ToArray();

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // cases.Document.CaseId has a real FK to cases.Case — without this check, a bogus
            // caseId doesn't fail here, it fails inside usp_Document_Save's INSERT with an
            // unhandled FK-violation exception, which ASP.NET Core turns into a bare 500 with no
            // body (by design, so it doesn't leak SQL internals) — the caller sees nothing useful.
            if (caseId is not null)
            {
                using var existsCmd = new SqlCommand("SELECT 1 FROM cases.[Case] WHERE CaseId = @CaseId", conn);
                existsCmd.Parameters.AddWithValue("@CaseId", caseId);
                if (await existsCmd.ExecuteScalarAsync() is null)
                    return Results.BadRequest($"CaseId {caseId} does not exist.");
            }

            using var cmd = new SqlCommand("[cases].[usp_Document_Save]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CaseId",       (object?)caseId ?? DBNull.Value);
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
