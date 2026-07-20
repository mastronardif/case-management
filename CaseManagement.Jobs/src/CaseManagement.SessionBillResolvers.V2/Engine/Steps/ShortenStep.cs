using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine.Steps;

// (S) — shorten a URL. Takes no chained doc input; url/lifetime come from wfParams
// (--url / --lifetime), same shape as (C) claim837P. Saves {shortUrl} as a tiny doc
// (consistent with every other step) and echoes the short URL to the console for
// one-liner copy/paste use.
public class ShortenStep(ICaseManagementRepository repository, ILogger<ShortenStep> logger) : IWorkflowStep
{
    public string Operator => "shorten";
    public OperatorInfo Info => Meta;
    public static OperatorInfo Meta { get; } = new("shorten",
        [
            new("url", "string", true, "URL to shorten"),
            new("lifetime", "int", false, "Hours until the short link expires; omit for no expiration")
        ]);

    public async Task<int[]> ExecuteAsync(int[] inputDocIds, string runId,
        IReadOnlyDictionary<string, JsonElement>? wfParams, CancellationToken ct)
    {
        if (wfParams is null || !wfParams.TryGetValue("url", out var urlElement) || urlElement.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("shorten: 'url' parameter is required");

        var url = urlElement.GetString()!;

        DateTime? expiresDate = null;
        if (wfParams.TryGetValue("lifetime", out var lifetimeElement) && lifetimeElement.ValueKind == JsonValueKind.Number)
            expiresDate = DateTime.UtcNow.AddHours(lifetimeElement.GetInt32());

        var shortLinkId = await repository.CreateShortLinkAsync(url, expiresDate, ct);

        var target = new Uri(url);
        var shortUrl = $"{target.Scheme}://{target.Authority}/s/{shortLinkId}";

        Console.WriteLine(shortUrl);
        logger.LogInformation("Shortened URL. shortLinkId={Id} target={Url} expires={Expires}",
            shortLinkId, url, expiresDate?.ToString("O") ?? "never");

        var docId = await repository.SaveDocumentAsync(
            new DocumentContext(),
            JsonSerializer.Serialize(new { shortUrl, targetUrl = url, expiresDate }),
            "shortLink", "shortLink.json", "application/json", ct);

        return [docId];
    }
}
