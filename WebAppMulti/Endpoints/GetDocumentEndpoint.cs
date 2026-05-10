using Microsoft.Data.SqlClient;
using System.Data;

public static class GetDocumentEndpoint
{
    public static void MapGetDocumentEndpoint(this WebApplication app)
    {
        app.MapGet("/api/corqs/getDocument", async (
            string documentId,
            IConfiguration config) =>
        {
            if (!int.TryParse(documentId, out var id))
                return Results.BadRequest("documentId must be an integer.");

            var connStr = config.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand("cases.usp_Document_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@DocumentId", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            if (!await reader.ReadAsync())
                return Results.NotFound($"Document {id} not found.");

            var contentType = reader.GetString(reader.GetOrdinal("ContentType"));
            var fileDataOrdinal = reader.GetOrdinal("FileData");
            var fileData = (byte[])reader.GetValue(fileDataOrdinal);

            return Results.File(fileData, contentType);
        });
    }
}
