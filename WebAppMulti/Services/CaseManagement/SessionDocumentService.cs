using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using WebAppMulti.Database.Dtos.CaseManagement;

namespace WebAppMulti.Services.CaseManagement
{
    public class SessionDocumentService : ISessionDocumentService
    {
        private readonly string _connString;

        public SessionDocumentService(IConfiguration config)
        {
            _connString = config.GetConnectionString("DefaultConnection");
        }

        // ======================================
        // SAVE DOCUMENT (Session Note)
        // ======================================
        public async Task SaveDocument(int sessionId, DocumentCreateDto dto)
        {
            using var conn = new SqlConnection(_connString);
            using var cmd = new SqlCommand("sp_SessionDocument_Save", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@SessionId", sessionId);
            cmd.Parameters.AddWithValue("@FormTemplateId", dto.FormTemplateId);
            cmd.Parameters.AddWithValue("@JsonData", JsonSerializer.Serialize(dto.FormData));

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ======================================
        // GET DOCUMENTS BY SESSION
        // ======================================
        public async Task<IEnumerable<SessionDocumentDto>> GetDocuments(int sessionId)
        {
            var results = new List<SessionDocumentDto>();

            using var conn = new SqlConnection(_connString);
            using var cmd = new SqlCommand("sp_SessionDocument_GetBySession", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@SessionId", sessionId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new SessionDocumentDto
                {
                    DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                    SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                    FormTemplateId = reader.GetInt32(reader.GetOrdinal("FormTemplateId")),
                    FormData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(reader.GetOrdinal("JsonData"))
                    )!
                });
            }

            return results;
        }
    }
}
