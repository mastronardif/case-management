using Microsoft.AspNetCore.Mvc;

namespace WebAppMulti.Controllers
{
    [ApiExplorerSettings(GroupName = "Case Management")]
    [ApiController]
    [Route("api/[controller]")]

    public class WorkbooksController : Controller
    {
        private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "Workbooks");

        //[HttpGet("{caseId}")]
        [HttpGet("GetBooks")]
        public IActionResult GetByCaseId(string caseId)
        {
            // Folder path: Workbooks/{caseId}
            var caseFolder = Path.Combine(_basePath, caseId);

            if (!Directory.Exists(caseFolder))
            {
                return NotFound(new { message = $"No folder found for caseId {caseId}" });
            }

            // Get list of files
            var files = Directory.GetFiles(caseFolder)
                .Select(f => new
                {
                    FileName = Path.GetFileName(f),
                    //FullPath = f,
                    CreatedOn = System.IO.File.GetCreationTime(f).ToString("MM/dd/yyyy"), // System.IO.File.GetCreationTime(f),
                    ModifiedOn = System.IO.File.GetLastWriteTime(f),
                    SizeBytes = new FileInfo(f).Length
                })
                .ToList();

            return Ok(files);
        }


        [HttpGet("GetBook")]
        public IActionResult DownloadFile(string caseId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(caseId) || string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { message = "caseId and fileName are required." });
            }

            // Build the full path safely
            var caseFolder = Path.Combine(_basePath, caseId);
            var filePath = Path.Combine(caseFolder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = $"File '{fileName}' not found for caseId '{caseId}'." });
            }

            // Read file bytes
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Determine MIME type
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            // Disable caching to prevent 304 responses
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // Return the file inline (no download forced)
            return File(fileBytes, contentType);
        }



        // Example: GET api/payerdocs/for?caseId=123&sessionIds=1&sessionIds=2
        //[HttpGet("for")]
        //public IActionResult PayerDocsFor([FromQuery] int caseId, [FromQuery] int[] sessionIds)
        //{
        //    // ✅ Normally you'd query a database here.
        //    // For demo, I'm mocking some data:
        //    var docs = sessionIds.Select((sid, index) => new
        //    {
        //        DocId = index + 1,
        //        Url = $"https://myserver.local/payerdocs/{caseId}/{sid}/doc{index + 1}.pdf"
        //    });

        //    return Ok(docs);
        //}


    }
}
