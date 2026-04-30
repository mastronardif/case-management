using WebAppMulti.Models.Corqs;

public static class CorqsEndpoints
{
    public static void MapCorqsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/corqs/{operation}", ExecuteAsync);
    }

    private static async Task<IResult> ExecuteAsync(
            string operation,
    Dictionary<string, object?> body,
    SchemaRegistry registry,
    CorqsExecutor executor)
        //string name,
        //HttpContext context,
        //SchemaRegistry registry,
        //CorqsExecutor executor)
    {
        var api = registry.GetApi(operation);

        if (api == null)
            return Results.NotFound(new { error = $"API '{operation}' not found." });

        var request = body ?? new();

        //var request =
        //    await context.Request.ReadFromJsonAsync<Dictionary<string, object?>>()
        //    ?? new Dictionary<string, object?>();

        var result = await executor.ExecuteAsync(api, request);

        return Results.Json(result);
    }
}
