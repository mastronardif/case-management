using Microsoft.AspNetCore.Mvc;

namespace WebAppMulti.Controllers.CaseManagement
{
    //[SwaggerTag("Case Management")]
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Case Management")]
    public class CalendarController : Controller
    {
        [HttpGet("GetCalendar")]
        public IActionResult GetCalendar(
            int year,
            int month)
        {
            // Demo-only mock data
            var events = new[]
            {
            new {
                id = Guid.NewGuid(),
                title = "ABA Session – Jake",
                start = new DateTime(year, month, 5, 15, 0, 0),
                end = new DateTime(year, month, 5, 17, 0, 0),
                rbt = "Kristine Mastronardi",
                status = "Completed"
            },
            new {
                id = Guid.NewGuid(),
                title = "ABA Session – Aliana",
                start = new DateTime(year, month, 12, 16, 0, 0),
                end = new DateTime(year, month, 12, 18, 0, 0),
                rbt = "John RBT",
                status = "Scheduled"
            }
        };  

            return Ok(events);
        }

    }
}
