using System.Text.RegularExpressions;
using WebAppMulti.Models.Corqs;

namespace WebAppMulti.Services.Corqs;

public class HttpProxyExecutor : IHttpProxyExecutor
{
    private readonly HttpClient _httpClient;

    public HttpProxyExecutor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<object?> ExecuteAsync(
        ApiDefinition api,
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(api.Url))
            throw new InvalidOperationException($"API '{api.Name}' does not define a URL.");

        var url = ReplaceRouteTokens(api.Url, parameters);
        var method = new HttpMethod(api.Method ?? "GET");

        using var request = new HttpRequestMessage(method, url);

        if (method != HttpMethod.Get && method != HttpMethod.Delete)
        {
            var body = parameters
                .Where(p => p.Value is not null)
                .ToDictionary(p => p.Key, p => p.Value);

            request.Content = JsonContent.Create(body);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken);
        return content;
    }

    private static string ReplaceRouteTokens(string url, IDictionary<string, object?> parameters)
    {
        return Regex.Replace(url, "\\{(.*?)\\}", match =>
        {
            var key = match.Groups[1].Value;
            return parameters.TryGetValue(key, out var value)
                ? Uri.EscapeDataString(value?.ToString() ?? string.Empty)
                : match.Value;
        });
    }
}
