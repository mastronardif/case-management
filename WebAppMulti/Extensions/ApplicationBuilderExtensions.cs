using WebAppMulti.Middleware;

namespace WebAppMulti.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseDynamicApi(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DynamicApiMiddleware>();
    }
}
