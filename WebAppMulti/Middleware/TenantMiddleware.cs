using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

// Single-tenant placeholder for now — reads a static value from config. Once real tenant
// identity exists (JWT claim, subdomain, API key, etc.), swap the config read below for that
// lookup; every response still gets stamped the same way, no other code needs to change.
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _tenantId;

    public TenantMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _tenantId = configuration["Tenant:Id"] ?? "default";
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["Tenant-Id"] = _tenantId;
            return Task.CompletedTask;
        });

        await _next(ctx);
    }
}
