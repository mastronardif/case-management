using System.Text.Json;

namespace WebAppMulti.Middleware;

public class DynamicApiMiddleware
{
    private readonly RequestDelegate _next;

    public DynamicApiMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/corqs"))
        {
            await _next(context);
            return;
        }

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                message = "Dynamic API middleware not implemented yet"
            }));

        return;
    }
}
