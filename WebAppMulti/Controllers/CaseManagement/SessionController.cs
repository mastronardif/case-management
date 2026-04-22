using Microsoft.AspNetCore.Mvc;
using WebAppMulti.Database.Dtos.CaseManagement;
using WebAppMulti.Services.CaseManagement;

namespace WebAppMulti.Controllers.CaseManagement
{
    //[SwaggerTag("Case Management")]
    [ApiController]
    [Route("api/cm/sessions")]
    [ApiExplorerSettings(GroupName = "Case Management")]
    public class SessionController : ControllerBase
    {
        private readonly SessionService _service;

        public SessionController(SessionService service)
        {
            _service = service;
        }

        /// <summary>
        /// Create a new session
        /// POST /api/cm/sessions
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSession(SessionCreateDto dto)
        {
            var id = await _service.CreateSession(dto);
            return Ok(new { sessionId = id });
        }

        /// <summary>
        /// Get sessions for a case by year and month
        /// GET /api/cm/sessions/{caseId}/{year}/{month}
        /// </summary>
        [HttpGet("{caseId}/{year}/{month}")]
        public async Task<IActionResult> GetSessionsByMonth(int caseId, int year, int month)
        {
            var sessions = await _service.GetSessionsByMonth(caseId, year, month);
            return Ok(sessions);
        }

        /// <summary>
        /// Add a document to a session
        /// POST /api/cm/sessions/{sessionId}/document
        /// </summary>
        [HttpPost("{sessionId}/document")]
        public async Task<IActionResult> AddDocument(int sessionId, DocumentCreateDto dto)
        {
            await _service.AddDocument(sessionId, dto);
            return Ok();
        }
    }
}
