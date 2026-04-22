
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;


public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    public RequestTimingMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();

        // Register callback to set the header just before the response starts
        ctx.Response.OnStarting(() =>
        {
            sw.Stop();
            ctx.Response.Headers["X-Request-Duration-ms"] = sw.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        await _next(ctx);
    }
}
