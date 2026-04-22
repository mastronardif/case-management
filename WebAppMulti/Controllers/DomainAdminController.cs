using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppMulti.Database;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DomainAdminController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly MyMarker _marker;

        public DomainAdminController(MyDbContext context, MyMarker marker)
        {
            _context = context;
            _marker = marker;
        }

        // Example: Get a combined report of customers and employees
        [HttpGet("dashboard-summary")]
        public async Task<ActionResult<object>> GetDashboardSummary()
        {
            var totalCustomers = await _context.DimCustomers.CountAsync();
            var totalEmployees = await _context.Employees.CountAsync();

            var recentCustomers = await _context.DimCustomers
                .OrderByDescending(c => c.CustomerKey)
                .Take(5)
                .Select(c => new { c.CustomerKey, c.FirstName, c.LastName, c.EmailAddress })
                .ToListAsync();

            var recentEmployees = await _context.Employees
                .OrderByDescending(e => e.EmployeeKey)
                .Take(5)
                .Select(e => new { e.EmployeeKey, e.FirstName, e.LastName })
                .ToListAsync();

            return Ok(new
            {
                Stats = new
                {
                    TotalCustomers = totalCustomers,
                    TotalEmployees = totalEmployees
                },
                RecentCustomers = recentCustomers,
                RecentEmployees = recentEmployees
            });
        }

        // Example: Search customers by last name
        [HttpGet("search-customers")]
        public async Task<ActionResult<List<object>>> SearchCustomers([FromQuery] string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                return BadRequest("Last name is required.");

            var customers = await _context.DimCustomers
                .Where(c => c.LastName.Contains(lastName))
                .Select(c => new
                {
                    c.CustomerKey,
                    c.FirstName,
                    c.LastName,
                    c.EmailAddress
                })
                .ToListAsync();

            return Ok(customers);
        }

        // Example: Delete a customer AND related domain logic
        [HttpDelete("delete-customer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.DimCustomers.FindAsync(id);
            if (customer == null)
                return NotFound();

            // You could add related business logic here, like logging or removing dependent records
            _context.DimCustomers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("collection-demo")]
        public IActionResult CollectionDemo()
        {
            var data = new List<int> { 1, 2, 3, 4, 5 };

            // IEnumerable: Works in memory
            IEnumerable<int> enumerable = data.Where(x => x > 2);

            // IQueryable: Simulated with AsQueryable
            IQueryable<int> queryable = data.AsQueryable().Where(x => x > 2);

            // List: Fully materialized
            List<int> list = data.Where(x => x > 2).ToList();

            return Ok(new
            {
                IEnumerable = enumerable,
                IQueryable = queryable,
                List = list
            });
        }

        //[HttpGet("sync-demo")]
        //public IActionResult SyncDemo()
        //{
        //    Thread.Sleep(2000); // Blocks thread
        //    return Ok("Sync complete");
        //}

        //[HttpGet("async-demo")]
        //public async Task<IActionResult> AsyncDemo()
        //{
        //    await Task.Delay(2000); // Non-blocking
        //    return Ok("Async complete");
        //}

        // Synchronous - Blocks the request thread
        //[HttpGet("sync-delay")]
        //public IActionResult SyncDelay()
        //{
        //    var start = DateTime.Now.ToString("HH:mm:ss.fff");
        //    Thread.Sleep(5000); // BLOCKS for 5 seconds
        //    var end = DateTime.Now.ToString("HH:mm:ss.fff");

        //    return Ok(new
        //    {
        //        Type = "Synchronous",
        //        StartTime = start,
        //        EndTime = end,
        //        Message = "Blocked the thread for 5 seconds"
        //    });
        //}

        //// Asynchronous - Does not block the request thread
        //[HttpGet("async-delay")]
        //public async Task<IActionResult> AsyncDelay()
        //{
        //    var start = DateTime.Now.ToString("HH:mm:ss.fff");
        //    await Task.Delay(5000); // Non-blocking wait for 5 seconds
        //    var end = DateTime.Now.ToString("HH:mm:ss.fff");

        //    return Ok(new
        //    {
        //        Type = "Asynchronous",
        //        StartTime = start,
        //        EndTime = end,
        //        Message = "Waited asynchronously for 5 seconds"
        //    });
        //}

        // sql

        // Synchronous (blocking)
        [HttpGet("slow-sync")]
        public IActionResult GetSlowSync()
        {
            // Execute raw SQL, fetch all results first
            var results = _context.Database
                .SqlQueryRaw<string>("WAITFOR DELAY '00:00:05'; SELECT 'Done Sync' AS Result")
                .AsEnumerable()
                .ToList();

            var firstResult = results.FirstOrDefault();

            return Ok(new
            {
                Mode = "Synchronous",
                Result = firstResult
            });
        }

        // Asynchronous (non-blocking)
        [HttpGet("slow-async")]
        public async Task<IActionResult> GetSlowAsync()
        {
            // Fetch all results asynchronously
            var results = await _context.Database
                .SqlQueryRaw<string>("WAITFOR DELAY '00:00:05'; SELECT 'Done Async' AS Result")
                .ToListAsync();

            var firstResult = results.FirstOrDefault();

            return Ok(new
            {
                Mode = "Asynchronous",
                Result = firstResult
            });
        }

        // test
        // New endpoint: call sync slow method N times sequentially (blocking)
        [HttpGet("call-sync-multiple/{count}")]
        public IActionResult CallSyncMultiple(int count)
        {
            _marker.Start();

            for (int i = 0; i < count; i++)
            {
                // Call sync method internally (blocking)
                var result = GetSlowSync() as OkObjectResult;
                if (result == null || result.StatusCode != 200)
                {
                    return StatusCode(result?.StatusCode ?? 500, $"Call {i + 1} failed");
                }
            }

            _marker.End();
            return Ok(new
            {
                data = new
                {
                    type = $"Completed {count} synchronous calls sequentially",
                    message = $"call-sync-multiple/{count}, {_marker.Report()}"
                }
            });

            //return Ok(new
            //{
            //    Type = $"Completed {count} synchronous calls sequentially",                
            //    Message = $"call-sync-multiple/{count}, {_marker.Report()}"
            //});
        }

        // New endpoint: call async slow method N times sequentially (non-blocking)
        [HttpGet("call-async-multiple/{count}")]
        public async Task<IActionResult> CallAsyncMultiple(int count)
        {
            var start = DateTime.Now.ToString("HH:mm:ss.fff");
            _marker.Start();

            for (int i = 0; i < count; i++)
            {
                var result = await GetSlowAsync() as OkObjectResult;
                if (result == null || result.StatusCode != 200)
                {
                    return StatusCode(result?.StatusCode ?? 500, $"Call {i + 1} failed");
                }
            }
            var end = DateTime.Now.ToString("HH:mm:ss.fff");
            _marker.End();

            return Ok(new
            {
                Type = $"Completed {count} asynchronous calls sequentially",
                StartTime = start,
                EndTime = end,
                Message = $" asynchronous calls sequentially/{count}, {_marker.Report()}"
            });
        }
    




}
}
