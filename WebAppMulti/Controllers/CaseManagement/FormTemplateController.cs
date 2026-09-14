using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebAppMulti.Helpers;
using WebAppMulti.Services.CaseManagement;

namespace WebAppMulti.Controllers.CaseManagement
{
    [ApiExplorerSettings(GroupName = "Case Management")]
    [ApiController]
    [Route("api/cm/form-templates")]
    public class FormTemplateController : ControllerBase
    {
        private readonly FormTemplateService _service;

        public FormTemplateController(FormTemplateService service)
        {
            _service = service;
        }

        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetTemplate(int id)
        //{
        //    var template = await _service.GetTemplate(id);
        //    return Ok(template);
        //}


        /// <summary>
        /// Get HTML template by ID and optionally fill with JSON data
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="jsonData">Optional JSON data as string</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHtmlFromTemplate(int id, [FromQuery] string? jsonData = null)
        {
            Dictionary<string, object>? data = null;

            if (!string.IsNullOrEmpty(jsonData))
            {
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
            }

            var html = await _service.GetHtmlFromTemplate(id, data);

            if (string.IsNullOrEmpty(html))
                return NotFound();

            return Content(html, "text/html");
        }

        [HttpGet("test/session/{sessionId}")]
        public async Task<IActionResult> GetTestRenderT(int sessionId)
        {
            string template = SessionRenderer.test_HtmlSession("", "");

            //var template = await _service.GetTemplate(sessionId);

            if (template == null)
                return NotFound($"Template {sessionId} not found.");

            return Ok(template);
        }


    }

}
