using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

public static class ResolveDocEndpoint
{
    public static void MapResolveDocEndpoint(this WebApplication app)
    {
        app.MapPost("/api/resolveDoc", async (ResolveDocRequest req, IConfiguration config) =>
        {
            if (!Regex.IsMatch(req.SpName ?? "", @"^[A-Za-z0-9_]+$"))
                return Results.BadRequest($"Invalid SP name '{req.SpName}'");

            if (req.DocId <= 0 || req.CaseId <= 0)
                return Results.BadRequest("docId and caseId are required");

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand($"[cases].[{req.SpName}]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@DocId",     req.DocId);
            cmd.Parameters.AddWithValue("@CaseId",    req.CaseId);
            cmd.Parameters.AddWithValue("@SessionId", (object?)req.SessionId ?? DBNull.Value);
            if (req.SrcDocId is not null)
                cmd.Parameters.AddWithValue("@SrcDocId", req.SrcDocId.Value);

            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { resolved = true, spName = req.SpName, docId = req.DocId, caseId = req.CaseId, srcDocId = req.SrcDocId });
        });
    }
}

public record ResolveDocRequest(int DocId, string SpName, int CaseId, int? SessionId = null, int? SrcDocId = null);
