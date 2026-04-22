using Microsoft.AspNetCore.Mvc;
using WebAppMulti.Database.Dtos.CaseManagement;
using WebAppMulti.Services.CaseManagement;

namespace WebAppMulti.Controllers.CaseManagement
{
    [ApiController]
    [Route("api/cm/session-documents")]
    [ApiExplorerSettings(GroupName = "Case Management")]
    public class SessionDocumentController : ControllerBase
    {
        private readonly SessionDocumentService _service;

        public SessionDocumentController(SessionDocumentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Save a document to a session
        /// POST /api/cm/session-documents/{sessionId}
        /// </summary>
        [HttpPost("{sessionId}")]
        public async Task<IActionResult> SaveDocument(int sessionId, DocumentCreateDto dto)
        {
            await _service.SaveDocument(sessionId, dto);
            return Ok();
        }

        /// <summary>
        /// Get all documents for a session
        /// GET /api/cm/session-documents/session/{sessionId}
        /// </summary>
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetBySession(int sessionId)
        {
            var docs = await _service.GetDocuments(sessionId);
            return Ok(docs);
        }
    }
}
