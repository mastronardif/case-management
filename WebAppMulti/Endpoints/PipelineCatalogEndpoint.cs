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

            // Pipelines — base rows only; template + paramsSchema live in the document at DocumentId
            using var pipeCmd = new SqlCommand("cases.usp_Pipeline_GetAll", conn) { CommandType = CommandType.StoredProcedure };
            using var reader  = await pipeCmd.ExecuteReaderAsync();
            var baseRows = new List<(int PipelineId, string Name, string? Description, int? DocId)>();
            while (await reader.ReadAsync())
            {
                var docIdOrdinal = reader.GetOrdinal("DocumentId");
                baseRows.Add((
                    PipelineId:  reader.GetInt32(reader.GetOrdinal("PipelineId")),
                    Name:        reader.GetString(reader.GetOrdinal("Name")),
                    Description: reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    DocId:       reader.IsDBNull(docIdOrdinal) ? null : reader.GetInt32(docIdOrdinal)
                ));
            }
            await reader.CloseAsync();

            var pipelines = new List<PipelineInfo>();
            foreach (var row in baseRows)
            {
                string? template = null, paramsSchemaJson = null;

                if (row.DocId is int docId)
                {
                    using var docCmd = new SqlCommand("cases.usp_Document_GetByContext", conn) { CommandType = CommandType.StoredProcedure };
                    docCmd.Parameters.AddWithValue("@DocumentId",   docId);
                    docCmd.Parameters.AddWithValue("@CaseId",       DBNull.Value);
                    docCmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
                    docCmd.Parameters.AddWithValue("@SessionId",    DBNull.Value);
                    docCmd.Parameters.AddWithValue("@DocumentType", DBNull.Value);
                    using var docReader = await docCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    if (await docReader.ReadAsync())
                    {
                        var fileData = (byte[])docReader.GetValue(docReader.GetOrdinal("FileData"));
                        var content  = System.Text.Encoding.UTF8.GetString(fileData);
                        template          = PipelineCatalogRenderer.ExtractJsonValue(content, "template");
                        paramsSchemaJson  = PipelineCatalogRenderer.ExtractJsonValue(content, "paramsSchema");
                    }
                    await docReader.CloseAsync();
                }

                pipelines.Add(new PipelineInfo(row.PipelineId, row.Name, row.Description, row.DocId, template, paramsSchemaJson));
            }

            // Operators — read from MyConstants "Operators" → docId → registry JSON (source of truth)
            string? operatorsJson = null;
            using var constCmd = new SqlCommand(
                "SELECT Value FROM [cases].[MyConstants] WHERE [Key] = 'Operators' AND [Type] = 'int'", conn);
            var constVal = await constCmd.ExecuteScalarAsync();
            if (constVal is not null and not DBNull)
            {
                var opDocId = Convert.ToInt32(constVal);
                using var opCmd = new SqlCommand("cases.usp_Document_GetByContext", conn) { CommandType = CommandType.StoredProcedure };
                opCmd.Parameters.AddWithValue("@DocumentId",   opDocId);
                opCmd.Parameters.AddWithValue("@CaseId",       DBNull.Value);
                opCmd.Parameters.AddWithValue("@WorkbookQId",  DBNull.Value);
                opCmd.Parameters.AddWithValue("@SessionId",    DBNull.Value);
                opCmd.Parameters.AddWithValue("@DocumentType", DBNull.Value);
                using var opReader = await opCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                if (await opReader.ReadAsync())
                {
                    var fileData = (byte[])opReader.GetValue(opReader.GetOrdinal("FileData"));
                    operatorsJson = System.Text.Encoding.UTF8.GetString(fileData);
                }
                await opReader.CloseAsync();
            }

            var html = PipelineCatalogRenderer.Render(pipelines, operatorsJson);
            return Results.Content(html, "text/html; charset=utf-8");
        });
    }
}
