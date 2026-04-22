using System.Diagnostics;

namespace WebAppMulti.Middleware
{

    public class BeforeAfterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BeforeAfterMiddleware> _logger;

        public BeforeAfterMiddleware(RequestDelegate next, ILogger<BeforeAfterMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Resolve Marker from the scoped request services
            var marker = context.RequestServices.GetRequiredService<MyMarker>();

            // BEFORE: logic before the request hits the next middleware/controller
            marker.Start();
            _logger.LogInformation("Before - Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);

            //var stopwatch = Stopwatch.StartNew();

            await _next(context); // Call the next middleware

            //stopwatch.Stop();
            marker.End();
            // AFTER: logic after the request is handled
            _logger.LogInformation("After - Response status: {StatusCode}, {report}",
                context.Response.StatusCode, marker.Report());
        }
    }

}