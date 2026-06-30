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
            await conn.OpenAsync();

            // Pipelines
            using var pipeCmd = new SqlCommand("cases.usp_Pipeline_GetAll", conn) { CommandType = CommandType.StoredProcedure };
            using var reader  = await pipeCmd.ExecuteReaderAsync();
            var pipelines = new List<PipelineInfo>();
            while (await reader.ReadAsync())
            {
                var docIdOrdinal = reader.GetOrdinal("DocumentId");
                pipelines.Add(new PipelineInfo(
                    PipelineId:   reader.GetInt32(reader.GetOrdinal("PipelineId")),
                    Name:         reader.GetString(reader.GetOrdinal("Name")),
                    Description:  reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    // TemplateJson: reader.GetString(reader.GetOrdinal("TemplateJson")),
                    // ParamsSchema: reader.GetString(reader.GetOrdinal("ParamsSchema")),
                    DocId:        reader.IsDBNull(docIdOrdinal) ? null : reader.GetInt32(docIdOrdinal)
                ));
            }
            await reader.CloseAsync();

            // Operators catalog (saved by: dotnet run -- --list-operators --html)
            string? operatorsJson = null;
            using var opCmd = new SqlCommand("cases.usp_Document_GetByContext", conn) { CommandType = CommandType.StoredProcedure };
            opCmd.Parameters.AddWithValue("@DocumentId",   DBNull.Value);
            opCmd.Parameters.AddWithValue("@CaseId",       DBNull.Value);
            opCmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
            opCmd.Parameters.AddWithValue("@SessionId",    DBNull.Value);
            opCmd.Parameters.AddWithValue("@DocumentType", "operators-catalog");
            using var opReader = await opCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            if (await opReader.ReadAsync())
            {
                var fileData = (byte[])opReader.GetValue(opReader.GetOrdinal("FileData"));
                operatorsJson = System.Text.Encoding.UTF8.GetString(fileData);
            }
            await opReader.CloseAsync();

            var html = PipelineCatalogRenderer.Render(pipelines, operatorsJson);
            return Results.Content(html, "text/html; charset=utf-8");
        });
    }
}
