using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebAppMulti.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayerDocsController : Controller
    {

        //public IActionResult Index()
        //{
        //    return View();
        //}


        [HttpPost("for")]
        public IActionResult PayerDocsFor([FromBody] JsonElement payload)
        {
            // Example: extract caseId and sessionIds from JSON
            if (!payload.TryGetProperty("caseId", out var caseIdProp) ||
                !payload.TryGetProperty("sessionIds", out var sessionIdsProp))
            {
                return BadRequest(new { error = "caseId and sessionIds are required" });
            }

            int caseId = caseIdProp.GetInt32();
            var sessionIds = sessionIdsProp.EnumerateArray().Select(x => x.GetInt32()).ToList();

            var docs = sessionIds.Select((sid, index) => new
            {
                DocId = index + 1,
                Url = "https://localhost:7009/api/Workbooks/GetBook?caseId=42&fileName=payerdoc.html"
                //Url = $"https://myserver.local/payerdocs/{caseId}/{sid}/doc{index + 1}.pdf"
            });

            return Ok(docs);
        }

    }
}
