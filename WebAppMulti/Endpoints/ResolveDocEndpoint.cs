using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

public static class ResolveDocEndpoint
{
    public static void MapResolveDocEndpoint(this WebApplication app)
    {
        app.MapPost("/api/resolveDoc", async (ResolveDocRequest req, IConfiguration config) =>
        {
            if (!Regex.IsMatch(req.TableName ?? "", @"^[A-Za-z0-9_]+$"))
                return Results.BadRequest($"Invalid table name '{req.TableName}'");

            if (req.DocId <= 0 || req.CaseId <= 0)
                return Results.BadRequest("docId and caseId are required");

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("[cases].[usp_CaseTable_Resolve]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TableName", req.TableName);
            cmd.Parameters.AddWithValue("@DocId",     req.DocId);
            cmd.Parameters.AddWithValue("@CaseId",    req.CaseId);
            cmd.Parameters.AddWithValue("@SrcDocId",  (object?)req.SrcDocId ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new { resolved = true, tableName = req.TableName, docId = req.DocId, caseId = req.CaseId, srcDocId = req.SrcDocId });
        });
    }
}

public record ResolveDocRequest(int DocId, string TableName, int CaseId, int? SrcDocId = null);
