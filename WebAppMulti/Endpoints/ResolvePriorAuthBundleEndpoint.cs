using Microsoft.Data.SqlClient;
using System.Data;

// Thin wrapper around usp_PriorAuthBundle_Resolve — the one place that knows PriorAuthForm and
// TreatmentPlanAuthorization are related (via TreatmentPlanAuthorization.PriorAuthFormId) lives in
// that SP's own T-SQL, not here and not in doc_extraction_agent.py. This endpoint just forwards
// the two already-extracted JSON doc ids and returns the two new row ids.
public static class ResolvePriorAuthBundleEndpoint
{
    public static void MapResolvePriorAuthBundleEndpoint(this WebApplication app)
    {
        app.MapPost("/api/resolvePriorAuthBundle", async (ResolvePriorAuthBundleRequest req, IConfiguration config) =>
        {
            if (req.CaseId <= 0)
                return Results.BadRequest("caseId is required");
            if (req.PriorAuthFormDocId <= 0 || req.TreatmentPlanDocId <= 0)
                return Results.BadRequest("priorAuthFormDocId and treatmentPlanDocId are required");

            var connStr = config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("[cases].[usp_PriorAuthBundle_Resolve]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CaseId", req.CaseId);
            cmd.Parameters.AddWithValue("@PriorAuthFormDocId", req.PriorAuthFormDocId);
            cmd.Parameters.AddWithValue("@PriorAuthFormSrcDocId", (object?)req.PriorAuthFormSrcDocId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TreatmentPlanDocId", req.TreatmentPlanDocId);
            cmd.Parameters.AddWithValue("@TreatmentPlanSrcDocId", (object?)req.TreatmentPlanSrcDocId ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.StatusCode(500);

            var priorAuthFormId = reader.GetInt32(reader.GetOrdinal("PriorAuthFormId"));
            var treatmentPlanAuthorizationId = reader.GetInt32(reader.GetOrdinal("TreatmentPlanAuthorizationId"));

            return Results.Ok(new { resolved = true, priorAuthFormId, treatmentPlanAuthorizationId });
        });
    }
}

public record ResolvePriorAuthBundleRequest(
    int CaseId,
    int PriorAuthFormDocId,
    int TreatmentPlanDocId,
    int? PriorAuthFormSrcDocId = null,
    int? TreatmentPlanSrcDocId = null);
