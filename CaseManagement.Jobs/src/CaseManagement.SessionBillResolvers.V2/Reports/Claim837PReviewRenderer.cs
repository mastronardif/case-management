using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CaseManagement.SessionBillResolvers.V2.Reports;

public static class Claim837PReviewRenderer
{
    private static readonly JsonSerializerOptions _indented = new() { WriteIndented = true };

    public static string Render(JsonObject claim, string runId)
    {
        var caseId    = claim["caseId"]?.GetValue<int>()    ?? 0;
        var sessionId = claim["sessionId"]?.GetValue<int>() ?? 0;
        var genAt     = claim["generatedAt"]?.GetValue<string>() ?? "";

        var session       = claim["session"]?.AsObject();
        var caseRow       = claim["case"]?.AsObject();
        var coverage      = claim["coverage"]?.AsArray()      ?? [];
        var authorization = claim["authorization"]?.AsArray() ?? [];
        var diagnoses     = claim["diagnoses"]?.AsArray()     ?? [];
        var provider      = claim["provider"]?.AsArray()      ?? [];
        var definition    = claim["definition"];

        var hasPrimary = diagnoses.Any(n =>
            n is JsonObject row &&
            row["isPrimary"] is JsonValue v &&
            (v.TryGetValue<bool>(out var b) && b || v.TryGetValue<int>(out var i) && i == 1));

        var checks = new (string Label, bool Ok, string Note)[]
        {
            ("Session",            session is not null,       ""),
            ("Patient / Case",     caseRow is not null,       ""),
            ("Coverage",           coverage.Count > 0,       coverage.Count > 0 ? $"{coverage.Count} record(s)" : "missing"),
            ("Authorization",      authorization.Count > 0,  authorization.Count > 0 ? $"{authorization.Count} active" : "no active authorization"),
            ("Diagnoses",          diagnoses.Count > 0,      diagnoses.Count > 0 ? $"{diagnoses.Count} code(s)" : "missing"),
            ("Primary Diagnosis",  hasPrimary,               ""),
            ("Billing Provider",   provider.Count > 0,       provider.Count > 0 ? $"{provider.Count} record(s)" : "missing"),
            ("837P Definition",    definition is not null,   ""),
        };

        var sb = new StringBuilder();
        sb.AppendLine($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <title>837P Claim Review &mdash; Case {{caseId}} / Session {{sessionId}}</title>
              <style>
                *, *::before, *::after { box-sizing: border-box; }
                body { font-family: Consolas, monospace; background: #f0f2f5; margin: 0; padding: 2rem; color: #222; }
                h1   { font-size: 1.1rem; margin: 0 0 0.4rem; }
                h2   { font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.06em; color: #999;
                       margin: 2rem 0 0.75rem; padding-bottom: 0.4rem; border-bottom: 1px solid #e2e8f0; }
                .meta { font-size: 0.8rem; color: #555; margin-bottom: 2rem; display: flex; flex-wrap: wrap; gap: 1.5rem; }
                .checks { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 0.5rem; }
                .check { display: flex; align-items: center; gap: 0.4rem; padding: 0.3rem 0.7rem;
                         border-radius: 6px; font-size: 0.78rem; border: 1px solid; }
                .ok   { background: #f0fdf4; border-color: #86efac; color: #166534; }
                .warn { background: #fef2f2; border-color: #fca5a5; color: #991b1b; }
                .note { font-size: 0.7rem; color: #888; margin-left: 0.3rem; }
                details { background: #fff; border: 1px solid #d8d8d8; border-radius: 6px;
                          box-shadow: 0 1px 3px rgba(0,0,0,.04); overflow: hidden; margin-bottom: 0.5rem; }
                summary { display: flex; align-items: center; gap: 1rem; padding: 0.6rem 1rem;
                          cursor: pointer; font-size: 0.82rem; user-select: none; list-style: none; }
                summary::-webkit-details-marker { display: none; }
                summary:hover { background: #f8fafc; }
                .sec-lbl   { font-weight: bold; color: #1e293b; flex: 1; }
                .sec-count { font-size: 0.7rem; color: #aaa; }
                .sec-body  { border-top: 1px solid #f1f5f9; padding: 1rem; }
                .kv        { display: grid; grid-template-columns: 15rem 1fr; gap: 0.2rem 1rem;
                             font-size: 0.78rem; margin-bottom: 0.75rem; }
                .kk { color: #888; }
                .kv-row-sep { grid-column: 1/-1; border-top: 1px solid #f1f5f9; margin: 0.5rem 0; }
                pre.doc-pre { background: #1e293b; color: #e2e8f0; padding: 1rem; border-radius: 4px;
                              font-size: 0.73rem; overflow-x: auto; max-height: 260px; overflow-y: auto;
                              white-space: pre; margin: 0.5rem 0 0; }
                .no-data { color: #aaa; font-size: 0.8rem; font-style: italic; }
              </style>
            </head>
            <body>
            """);

        sb.AppendLine($"<h1>837P Claim Review &mdash; Case {caseId} / Session {sessionId}</h1>");
        sb.AppendLine($"""
            <div class="meta">
              <span><strong>RunId:</strong> {HE(runId)}</span>
              <span><strong>Generated:</strong> {HE(genAt)}</span>
            </div>
            """);

        sb.AppendLine("<h2>Validation</h2><div class=\"checks\">");
        foreach (var (label, ok, note) in checks)
        {
            var noteHtml = note.Length > 0 ? $"<span class='note'>{HE(note)}</span>" : "";
            sb.AppendLine($"  <div class=\"check {(ok ? "ok" : "warn")}\">{(ok ? "✓" : "⚠")} {HE(label)}{noteHtml}</div>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>Data</h2>");
        AppendObjectSection(sb, "Session",         session,       open: true);
        AppendObjectSection(sb, "Patient / Case",  caseRow,       open: true);
        AppendArraySection(sb,  "Coverage",        coverage,      open: true);
        AppendArraySection(sb,  "Authorization",   authorization, open: true);
        AppendArraySection(sb,  "Diagnoses",       diagnoses,     open: true);
        AppendArraySection(sb,  "Provider",        provider,      open: true);
        AppendNodeSection(sb,   "837P Definition", definition,    open: false);

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendObjectSection(StringBuilder sb, string label, JsonObject? obj, bool open)
    {
        sb.AppendLine($"<details{(open ? " open" : "")}><summary><span class=\"sec-lbl\">{HE(label)}</span></summary>");
        sb.AppendLine("<div class=\"sec-body\">");
        if (obj is null) sb.AppendLine("<div class=\"no-data\">No data</div>");
        else             AppendRow(sb, obj);
        sb.AppendLine("</div></details>");
    }

    private static void AppendArraySection(StringBuilder sb, string label, JsonArray arr, bool open)
    {
        sb.AppendLine($"<details{(open ? " open" : "")}><summary><span class=\"sec-lbl\">{HE(label)}</span><span class=\"sec-count\">{arr.Count} row(s)</span></summary>");
        sb.AppendLine("<div class=\"sec-body\">");
        if (arr.Count == 0)
        {
            sb.AppendLine("<div class=\"no-data\">No data</div>");
        }
        else
        {
            bool first = true;
            foreach (var node in arr)
            {
                if (!first) sb.AppendLine("<div class=\"kv-row-sep\"></div>");
                if (node is JsonObject row) AppendRow(sb, row);
                first = false;
            }
        }
        sb.AppendLine("</div></details>");
    }

    private static void AppendNodeSection(StringBuilder sb, string label, JsonNode? node, bool open)
    {
        sb.AppendLine($"<details{(open ? " open" : "")}><summary><span class=\"sec-lbl\">{HE(label)}</span></summary>");
        sb.AppendLine("<div class=\"sec-body\">");
        if (node is null)
        {
            sb.AppendLine("<div class=\"no-data\">Not loaded</div>");
        }
        else
        {
            AppendJsonPre(sb, node);
        }
        sb.AppendLine("</div></details>");
    }

    private static void AppendRow(StringBuilder sb, JsonObject row)
    {
        var scalars = row.Where(kvp => kvp.Key != "doc" && kvp.Value is JsonValue).ToList();
        var doc     = row["doc"];

        if (scalars.Count > 0)
        {
            sb.AppendLine("<div class=\"kv\">");
            foreach (var (k, v) in scalars)
                sb.AppendLine($"  <span class=\"kk\">{HE(k)}</span><span>{HE(v?.ToString() ?? "")}</span>");
            sb.AppendLine("</div>");
        }

        if (doc is not null)
            AppendJsonPre(sb, doc);
    }

    private static void AppendJsonPre(StringBuilder sb, JsonNode node)
    {
        var json    = node.ToJsonString(_indented);
        var lines   = json.Split('\n');
        var preview = string.Join('\n', lines.Take(60)) +
                      (lines.Length > 60 ? $"\n… ({lines.Length} lines total)" : "");
        sb.AppendLine($"<pre class=\"doc-pre\">{HE(preview)}</pre>");
    }

    private static string HE(string? s) => WebUtility.HtmlEncode(s ?? "");
}
