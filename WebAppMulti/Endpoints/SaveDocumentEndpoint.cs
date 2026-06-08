using Microsoft.Data.SqlClient;
using System.Data;

public static class SaveDocumentEndpoint
{
    public static void MapSaveDocumentEndpoint(this WebApplication app)
    {
        app.MapPost("/api/saveDocument", async (SaveDocumentRequest req, IConfiguration config) =>
        {
            if (req.SourceDocId <= 0)
                return Results.BadRequest("sourceDocId is required");

            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(req.Json ?? "{}");
            var connStr   = config.GetConnectionString("DefaultConnection");

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            int? caseId = null, sessionId = null;
            using (var getCmd = new SqlCommand("cases.usp_Document_GetByContext", conn)
                   { CommandType = CommandType.StoredProcedure })
            {
                getCmd.Parameters.AddWithValue("@DocumentId",   req.SourceDocId);
                getCmd.Parameters.AddWithValue("@CaseId",       DBNull.Value);
                getCmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
                getCmd.Parameters.AddWithValue("@SessionId",    DBNull.Value);
                getCmd.Parameters.AddWithValue("@DocumentType", DBNull.Value);

                using var r = await getCmd.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                    return Results.NotFound($"Source document {req.SourceDocId} not found");

                caseId    = r.IsDBNull(r.GetOrdinal("CaseId"))    ? null : r.GetInt32(r.GetOrdinal("CaseId"));
                sessionId = r.IsDBNull(r.GetOrdinal("SessionId")) ? null : r.GetInt32(r.GetOrdinal("SessionId"));
            }

            int newDocId;
            using (var saveCmd = new SqlCommand("[cases].[usp_Document_Save]", conn)
                   { CommandType = CommandType.StoredProcedure })
            {
                saveCmd.Parameters.AddWithValue("@CaseId",       (object?)caseId    ?? DBNull.Value);
                saveCmd.Parameters.AddWithValue("@SessionId",    (object?)sessionId ?? DBNull.Value);
                saveCmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
                saveCmd.Parameters.AddWithValue("@CaseNumber",   DBNull.Value);
                saveCmd.Parameters.AddWithValue("@DocumentType", "sessionCorrected");
                saveCmd.Parameters.AddWithValue("@Title",        "sessionCorrected");
                saveCmd.Parameters.AddWithValue("@FileName",     "sessionCorrected.json");
                saveCmd.Parameters.AddWithValue("@ContentType",  "application/json");
                saveCmd.Parameters.AddWithValue("@FileData",     jsonBytes);
                saveCmd.Parameters.AddWithValue("@CreatedBy",    "review-ui");

                var docIdParam = new SqlParameter("@DocumentId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value     = DBNull.Value
                };
                saveCmd.Parameters.Add(docIdParam);

                await saveCmd.ExecuteNonQueryAsync();
                newDocId = (int)docIdParam.Value;
            }

            return Results.Ok(new { docId = newDocId });
        });
    }
}

public record SaveDocumentRequest(int SourceDocId, string? Json);
