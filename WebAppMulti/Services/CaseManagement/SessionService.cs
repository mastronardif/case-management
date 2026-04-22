using System.Data;
using WebAppMulti.Database.Dtos.CaseManagement;
using WebAppMulti.Database.Models.CaseManagement;
using WebAppMulti.Database.Repository;

namespace WebAppMulti.Services.CaseManagement
{
    public class SessionService
    {
        private readonly GenericRepository _repo;

        public SessionService(GenericRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Create a session (calls SP "sp_CreateSession")
        /// </summary>
        public async Task<int> CreateSession(SessionCreateDto dto)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@CaseId", dto.CaseId },
                { "@SessionDate", dto.SessionDate },
                { "@Notes", dto.Notes ?? "" }
            };

            var dt = await _repo.ExecuteStoredProcedureAsync("sp_CreateSession", parameters);

            // Assuming SP returns new SessionId
            if (dt.Rows.Count > 0 && dt.Columns.Contains("SessionId"))
                return Convert.ToInt32(dt.Rows[0]["SessionId"]);

            return 0;
        }

        /// <summary>
        /// Get sessions by month (calls SP "sp_GetSessionsByMonth")
        /// </summary>
        public async Task<List<SessionDto>> GetSessionsByMonth(int caseId, int year, int month)
        {
            var parameters = new Dictionary<string, object>
    {
        { "@CaseId", caseId },
        { "@Year", year },
        { "@Month", month }
    };

            // Call your stored procedure
            var dt = await _repo.ExecuteStoredProcedureAsync("sp_GetSessionsByMonth", parameters);

            var sessions = new List<SessionDto>();

            foreach (DataRow row in dt.Rows)
            {
                sessions.Add(new SessionDto
                {
                    SessionId = Convert.ToInt32(row["SessionId"]),
                    CaseId = Convert.ToInt32(row["CaseId"]),
                    SessionDate = Convert.ToDateTime(row["SessionDate"]),
                    Notes = row["Notes"]?.ToString(),
                    // FIX: Use DocumentDto to match your DTO
                    Documents = new List<DocumentDto>() // empty placeholder, will fill from SP later
                });
            }

            return sessions;
        }

        /// <summary>
        /// Add document to a session (calls SP "sp_AddSessionDocument")
        /// </summary>
        public async Task AddDocument(int sessionId, DocumentCreateDto dto)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@SessionId", sessionId },
                { "@FormTemplateId", dto.FormTemplateId },
                { "@FormData", System.Text.Json.JsonSerializer.Serialize(dto.FormData) }
            };

            await _repo.ExecuteStoredProcedureAsync("sp_AddSessionDocument", parameters);
        }

        /// <summary>
        /// Get form template by ID (returns placeholder)
        /// </summary>
        public async Task<FormTemplate> GetTemplate(int id)
        {
            // Later call SP to get template
            await Task.CompletedTask;
            return new FormTemplate
            {
                FormTemplateId = id,
                Name = "",
                Description = "",
                JsonSchema = "{}",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
