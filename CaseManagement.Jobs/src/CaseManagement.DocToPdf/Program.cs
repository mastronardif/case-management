// DocToPdf — renders any page (a doc-store report via --doc-id, or any full URL via --url)
// to a real PDF using headless Chromium (Playwright), instead of the browser's manual
// print-to-PDF. Since these reports are already self-contained HTML/CSS (Cms1500Renderer,
// AvailityClaimFormRenderer, etc.), navigating a real browser engine to the exact same URL
// the app already serves and printing it is the most faithful, lowest-effort way to get a
// PDF — no re-implementing report layouts in a PDF-drawing library.
//
// Usage:
//   dotnet run -- --doc-id 3919
//   dotnet run -- --doc-id 3919 --out "C:\temp\availity-preview.pdf"
//   dotnet run -- --url "http://localhost:5173/docviewer/3919" --out review.pdf
//   dotnet run -- --doc-id 3919 --base-url https://localhost:44344

using Microsoft.Playwright;

string? docId = null;
string? url = null;
string? outPath = null;
string baseUrl = "http://localhost:5173";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--doc-id": docId = args[++i]; break;
        case "--url": url = args[++i]; break;
        case "--out": outPath = args[++i]; break;
        case "--base-url": baseUrl = args[++i].TrimEnd('/'); break;
        case "--help":
            Console.WriteLine("""
                DocToPdf — render a page to PDF via headless Chromium.

                  --doc-id <id>     Render http://<base-url>/api/getDocument?docId=<id>
                  --url <url>       Render this exact URL instead (any page, not just a doc)
                  --out <path>      Output PDF path (default: .\output\doc-<id>.pdf or page.pdf)
                  --base-url <url>  Default: http://localhost:5173 (the Vite dev proxy)
                """);
            return 0;
    }
}

if (docId is null && url is null)
{
    Console.Error.WriteLine("Provide --doc-id or --url. See --help.");
    return 1;
}

var targetUrl = url ?? $"{baseUrl}/api/getDocument?docId={docId}";

outPath ??= docId is not null
    ? Path.Combine("output", $"doc-{docId}.pdf")
    : Path.Combine("output", "page.pdf");

var outDir = Path.GetDirectoryName(Path.GetFullPath(outPath));
if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

Console.WriteLine($"Rendering: {targetUrl}");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 1280, Height = 1024 } });

await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

await page.PdfAsync(new PagePdfOptions
{
    Path = outPath,
    Format = "Letter",
    PrintBackground = true,
    Margin = new Margin { Top = "0.4in", Bottom = "0.4in", Left = "0.4in", Right = "0.4in" },
});

Console.WriteLine($"Wrote: {Path.GetFullPath(outPath)}");
return 0;
