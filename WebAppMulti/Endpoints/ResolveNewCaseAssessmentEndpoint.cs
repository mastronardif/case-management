using Microsoft.Data.SqlClient;
using System.Data;

// Thin wrapper around usp_NewCase_Assessment_Resolve — the one place that creates Case, backfills
// the already-uploaded source Document's CaseId, and resolves Assessment, all in one transaction.
// Never called with an existing CaseId — that's still the ordinary /api/resolveDoc path, unchanged.
public static class ResolveNewCaseAssessmentEndpoint
{
    public static void MapResolveNewCaseAssessmentEndpoint(this WebApplication app)
    {
        app.MapPost("/api/resolveNewCaseAssessment", async (ResolveNewCaseAssessmentRequest req, IConfiguration config) =>
        {
            if (req.SourceDocumentId <= 0 || req.AssessmentJsonDocId <= 0)
                return Results.BadRequest("sourceDocumentId and assessmentJsonDocId are required");

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("[cases].[usp_NewCase_Assessment_Resolve]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@SourceDocumentId", req.SourceDocumentId);
            cmd.Parameters.AddWithValue("@AssessmentJsonDocId", req.AssessmentJsonDocId);
            cmd.Parameters.AddWithValue("@CaseTitle", (object?)req.CaseTitle ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.StatusCode(500);

            var caseId = reader.GetInt32(reader.GetOrdinal("CaseId"));
            var caseNumber = reader.GetString(reader.GetOrdinal("CaseNumber"));
            var assessmentId = reader.GetInt32(reader.GetOrdinal("AssessmentId"));

            return Results.Ok(new { resolved = true, caseId, caseNumber, assessmentId });
        });
    }
}

public record ResolveNewCaseAssessmentRequest(int SourceDocumentId, int AssessmentJsonDocId, string? CaseTitle = null);
