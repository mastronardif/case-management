using Microsoft.Data.SqlClient;
using System.Data;
using WebAppMulti.Reports;

public static class PipelineCatalogEndpoint
{
    public static void MapPipelineCatalogEndpoint(this WebApplication app)
    {
        app.MapGet("/api/pipelineCatalog", async (IConfiguration config) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection");
            using var conn = new SqlConnection(connStr);
            using var cmd  = new SqlCommand("cases.usp_Pipeline_GetAll", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var pipelines = new List<PipelineInfo>();
            while (await reader.ReadAsync())
            {
                pipelines.Add(new PipelineInfo(
                    PipelineId:   reader.GetInt32(reader.GetOrdinal("PipelineId")),
                    Name:         reader.GetString(reader.GetOrdinal("Name")),
                    Description:  reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    TemplateJson: reader.GetString(reader.GetOrdinal("TemplateJson")),
                    ParamsSchema: reader.GetString(reader.GetOrdinal("ParamsSchema"))
                ));
            }

            var html = PipelineCatalogRenderer.Render(pipelines);
            return Results.Content(html, "text/html; charset=utf-8");
        });
    }
}
