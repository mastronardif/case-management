using WebAppMulti.Models.Corqs;

namespace WebAppMulti.Endpoints.Corqs;

public static class CorqsEndpoints
{
    public static void MapCorqsEndpoints(this WebApplication app)
    {
        //app.MapPost("/api/corqs/{operation}", ExecuteAsync);
        app.MapMethods(
    "/api/corqs/{operation}",
    new[] { "GET", "POST" },
    ExecuteAsync);

    }

    private static async Task<IResult> ExecuteAsync(
        string operation,
        HttpRequest request,
        SchemaRegistry registry,
        CorqsExecutor executor)
    {
        var api = registry.GetApi(operation);

        if (api is null)
        {
            return Results.NotFound(new
            {
                error = $"API '{operation}' not found."
            });
        }

        var input = new Dictionary<string, object?>(
            StringComparer.OrdinalIgnoreCase);

        // Merge query string parameters
        foreach (var q in request.Query)
        {
            input[q.Key] = q.Value.ToString();
        }

        // Merge JSON body if present
        if (request.ContentLength > 0 &&
            request.HasJsonContentType())
        {
            var body = await request.ReadFromJsonAsync<Dictionary<string, object?>>();
            if (body is not null)
            {
                foreach (var kv in body)
                {
                    input[kv.Key] = kv.Value;
                }
            }
        }

        var result = await executor.ExecuteAsync(api, input);

        return Results.Json(result);
    }
}
