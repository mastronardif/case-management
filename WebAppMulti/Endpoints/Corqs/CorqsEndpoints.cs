using System.Text.Json;

public static class CorqsEndpoints
{
    public static void MapCorqsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/corqs", ExecuteAsync);
    }

    private static async Task<IResult> ExecuteAsync(
        HttpRequest request,
        SchemaRegistry registry,
        CorqsExecutor executor)
    {
        var body = await request.ReadFromJsonAsync<Dictionary<string, object?>>();

        if (body == null)
            return Results.BadRequest("Invalid JSON body");

        if (!body.TryGetValue("action", out var actionObj))
            return Results.BadRequest("Missing 'action'");

        var action = actionObj?.ToString();

        if (string.IsNullOrWhiteSpace(action))
            return Results.BadRequest("Empty action");

        var api = registry.GetApi(action);   // ? FIXED HERE

        if (api is null)
        {
            return Results.NotFound(new
            {
                error = $"API '{action}' not found."
            });
        }

        Dictionary<string, object?> input = new();

        if (body.TryGetValue("params", out var p) && p is JsonElement json)
        {
            input = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                json.GetRawText()
            ) ?? new();
        }

        var result = await executor.ExecuteAsync(api, input);

        return Results.Ok(result);
    }

}
