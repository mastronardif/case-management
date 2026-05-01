using System.Text.Json;
using WebAppMulti.Models.Corqs;

public class HttpExecutionStrategy : ICorqsExecutionStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string Type => "http";

    public HttpExecutionStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<object> ExecuteAsync(
        ApiDefinition api,
        Dictionary<string, object?> input)
    {
        if (string.IsNullOrWhiteSpace(api.Url))
            throw new InvalidOperationException(
                $"HTTP API '{api.Name}' is missing a Url.");

        var client = _httpClientFactory.CreateClient();

        var method = (api.Method ?? "GET").ToUpperInvariant();
        var url = api.Url;

        foreach (var kv in input)
        {
            var token = $"{{{kv.Key}}}";

            url = url.Replace(
                token,
                Uri.EscapeDataString(kv.Value?.ToString() ?? string.Empty),
                StringComparison.OrdinalIgnoreCase);
        }

        HttpResponseMessage response = method switch
        {
            "GET" => await client.GetAsync(url),
            "POST" => await client.PostAsJsonAsync(url, input),
            _ => throw new NotSupportedException(
                $"HTTP method '{method}' is not supported.")
        };

        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[CORQS HTTP] URL: {url}");
        Console.WriteLine($"[CORQS HTTP] Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"[CORQS HTTP] Body: {content}");

        object? data;
        try
        {
            data = string.IsNullOrWhiteSpace(content)
                ? null
                : JsonSerializer.Deserialize<JsonElement>(content);
        }
        catch
        {
            data = content;
        }

        return new
        {
            success = response.IsSuccessStatusCode,
            statusCode = (int)response.StatusCode,
            reasonPhrase = response.ReasonPhrase,
            data
        };
    }

}
