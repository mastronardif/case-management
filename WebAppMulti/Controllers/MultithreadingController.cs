using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebAppMulti.Controllers
{


    [ApiController]
    [Route("[controller]")]
    public class MultithreadController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MultithreadController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("parallel-api-calls")]
        public async Task<IActionResult> GetParallelData()
        {
            var client = _httpClientFactory.CreateClient();

            // Simulate two API calls
            //var task1 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
            //var task2 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/2");
            //var task3 = client.GetStringAsync("https://jsonplaceholder.typicode.com/photos");

            //await Task.WhenAll(task1, task2);

            //return Ok(new { Post1 = task1.Result, Post2 = task2.Result });

            var urls = new[]
                {
                    "https://jsonplaceholder.typicode.com/posts/1",
                    "https://jsonplaceholder.typicode.com/posts/2",
                    "https://jsonplaceholder.typicode.com/photos"
                };

            var tasks = urls.Select(async url =>
            {
                try
                {
                    return await client.GetStringAsync(url);
                }
                catch (Exception ex)
                {
                    // Log the error as needed
                    return $"Error: {ex.Message}";
                }
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            return Ok(new { Post1 = results[0], Post2 = results[1], Photos = results[2] });
        }

        [HttpGet("cpu-bound-task")]
        public async Task<IActionResult> RunCpuBoundTask()
        {
            var stopwatch = Stopwatch.StartNew();

            var result = await Task.Run(() =>
            {
                // Simulate CPU-intensive work
                double sum = 0;
                for (int i = 0; i < 10_000_000; i++)
                    sum += Math.Sqrt(i);
                return sum;
            });

            stopwatch.Stop();
            return Ok(new { Result = result, TimeElapsedMs = stopwatch.ElapsedMilliseconds });
        }
    }

}
