using Microsoft.AspNetCore.Mvc;

namespace WebAppMulti.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;

        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }


        // Catch-all route to capture the entire encoded URL
        [HttpGet("{*encodedUrl}")]
        public async Task<IActionResult> Get(string encodedUrl)
        {
            if (string.IsNullOrWhiteSpace(encodedUrl))
                return BadRequest("Missing target URL");

            try
            {
                // Decode the URL from path
                var targetUrl = System.Web.HttpUtility.UrlDecode(encodedUrl);

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(targetUrl);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Error fetching external page");

                var html = await response.Content.ReadAsStringAsync();
                // point to this domain
                // 🔹 Rewrite the form action so it posts back to *your* API
                html = html.Replace(
                    "action=\"http://localhost:8080/cgi-bin/cgi/ngfop/each.pl\"",
                    "action=\"/mypost\""
                );


                return Content(html, "text/html");
            }
            catch
            {
                return StatusCode(500, "Error fetching external page");
            }
        }


        // POST /Proxy/mypost
        [HttpPost("mypost")]
        public async Task<IActionResult> Post([FromForm] Dictionary<string, string> formData)
        {
            if (formData == null || formData.Count == 0)
                return BadRequest("No form data received");

            try
            {
                var client = _httpClientFactory.CreateClient();

                // Forward form data to original CGI script
                var content = new FormUrlEncodedContent(formData);
                var response = await client.PostAsync("http://localhost:8080/cgi-bin/cgi/ngfop/each.pl", content);

                var body = await response.Content.ReadAsStringAsync();

                // Send CGI response back to browser
                return Content(body, response.Content.Headers.ContentType?.ToString() ?? "text/html");
            }
            catch
            {
                return StatusCode(500, "Error forwarding POST to CGI script");
            }
        }



    }
}
