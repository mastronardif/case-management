using Microsoft.AspNetCore.Mvc;

namespace WebAppMulti.Controllers
{
    public class CgiController : Controller
    {

        // POST /each
        [HttpPost]
        public IActionResult Post([FromForm] Dictionary<string, string> formData)
        {
            if (formData == null || formData.Count == 0)
                return BadRequest("No form data received");

            // Example: do something with the form data
            // You could log it, save to DB, or return it back
            Console.WriteLine("Received form data:");
            foreach (var kvp in formData)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }   

            return Ok(new
            {
                message = "Form received successfully",
                received = formData
            });
        }

    }
}
