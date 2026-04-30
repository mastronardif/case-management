using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebAppMulti.Database.Repository;
using System.Text.Json;

namespace WebAppMulti.Modules.Corqs;

public static class CorqsEndpoints
{
    //public static void MapCorqsEndpoints(this WebApplication app)
    //{
    //    app.MapPost("/api/corqs/{operation}", ExecuteAsync);
    //}

    private static async Task<IResult> ExecuteAsync(
        string operation,
        HttpContext context,
        SchemaRegistry schema,
        CorqsExecutor executor)
    {
        var api = schema.GetApi(operation);

        if (api == null)
        {
            return Results.NotFound(new
            {
                error = $"Operation '{operation}' not found"
            });
        }

        var payload = await context.Request
            .ReadFromJsonAsync<Dictionary<string, object?>>()
            ?? new Dictionary<string, object?>();

        var result = await executor.ExecuteAsync(api, payload);

        return Results.Json(result);
    }
}
