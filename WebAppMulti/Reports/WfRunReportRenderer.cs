using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace WebAppMulti.Reports;

public static class WfRunReportRenderer
{
    public static string Render(string manifestJson)
    {
        var manifest      = JsonNode.Parse(manifestJson)!.AsObject();
        var runId         = manifest["runId"]?.GetValue<string>() ?? "?";
        var workflowId    = manifest["workflowId"]?.GetValue<string>() ?? "?";
        var version       = manifest["version"]?.ToString() ?? "?";
        var workflowDocId = manifest["workflowDocId"]?.GetValue<int>() ?? 0;
        var startedAt     = manifest["startedAt"]?.GetValue<DateTime>() ?? DateTime.MinValue;
        var completedAt   = manifest["completedAt"]?.GetValue<DateTime>() ?? DateTime.MinValue;
        var duration      = completedAt - startedAt;
        var steps         = manifest["steps"]?.AsArray() ?? [];

        // Collect all unique docs across all steps (preserving first-seen name)
        var docs = new Dictionary<int, string>();
        docs[workflowDocId] = "workflow.json";
        foreach (var stepNode in steps)
        {
            var step        = stepNode!.AsObject();
            var inTokens    = step["inputTokens"]?.AsArray()    ?? [];
            var outNames    = step["outputNames"]?.AsArray()    ?? [];
            var inResolved  = step["resolvedInputs"]?.AsArray() ?? [];
            var outResolved = step["resolvedOutputs"]?.AsArray() ?? [];

            for (int i = 0; i < inTokens.Count; i++)
            {
                var token = inTokens[i]?.GetValue<string>() ?? "";
                var parts = token.Split(' ', 2);
                var name  = parts.Length > 1 ? parts[1] : token;
                var docId = i < inResolved.Count && inResolved[i] != null
                    ? (int?)inResolved[i]!.GetValue<int>() : null;
                if (docId.HasValue && !docs.ContainsKey(docId.Value))
                    docs[docId.Value] = name;
            }
            for (int i = 0; i < outNames.Count; i++)
            {
                var name  = outNames[i]?.GetValue<string>() ?? "?";
                var docId = i < outResolved.Count && outResolved[i] != null
                    ? (int?)outResolved[i]!.GetValue<int>() : null;
                if (docId.HasValue && !docs.ContainsKey(docId.Value))
                    docs[docId.Value] = name;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <title>Workflow Run Report</title>
              <style>
                *, *::before, *::after { box-sizing: border-box; }
                body { font-family: Consolas, monospace; background: #f0f2f5; margin: 0; padding: 2rem; color: #222; }
                h1 { font-size: 1.1rem; margin: 0 0 0.4rem; }
                h2 { font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.06em; color: #999;
                     margin: 2rem 0 0.75rem; padding-bottom: 0.4rem; border-bottom: 1px solid #e2e8f0; }
                .meta { font-size: 0.8rem; color: #555; margin-bottom: 2rem; display: flex; flex-wrap: wrap; gap: 1.5rem; }
                .pipeline { display: flex; align-items: flex-start; overflow-x: auto; padding-bottom: 1rem; }
                .arrow { align-self: center; font-size: 1.6rem; color: #bbb; padding: 0 0.5rem; }
                .step { background: #fff; border: 1px solid #d8d8d8; border-radius: 8px; padding: 1rem 1.2rem;
                        width: 260px; flex-shrink: 0; box-shadow: 0 1px 3px rgba(0,0,0,0.06); }
                .step-id { font-weight: bold; font-size: 0.9rem; margin-bottom: 0.1rem; }
                .step-op { font-size: 0.75rem; color: #888; border-bottom: 1px solid #eee;
                           padding-bottom: 0.6rem; margin-bottom: 0.6rem; }
                .lbl { font-size: 0.65rem; text-transform: uppercase; letter-spacing: 0.06em;
                       color: #bbb; margin: 0.6rem 0 0.25rem; }
                ul { list-style: none; margin: 0; padding: 0; }
                li { font-size: 0.78rem; margin-bottom: 0.25rem; display: flex; gap: 0.4rem; flex-wrap: wrap; align-items: baseline; }
                .dn { color: #444; }
                .di { color: #aaa; font-size: 0.7rem; }
                a { color: #1a6fbd; text-decoration: none; }
                a:hover { text-decoration: underline; }

                /* Documents section */
                .doc-list { display: flex; flex-direction: column; gap: 0.5rem; }
                details { background: #fff; border: 1px solid #d8d8d8; border-radius: 6px;
                          box-shadow: 0 1px 3px rgba(0,0,0,0.04); overflow: hidden; }
                summary { display: flex; align-items: center; gap: 1rem; padding: 0.6rem 1rem;
                          cursor: pointer; font-size: 0.82rem; user-select: none; list-style: none; }
                summary::-webkit-details-marker { display: none; }
                summary:hover { background: #f8fafc; }
                .doc-id   { color: #1a6fbd; font-weight: bold; min-width: 3rem; }
                .doc-name { color: #444; flex: 1; }
                .doc-toggle { font-size: 0.7rem; color: #aaa; margin-left: auto; }
                details[open] .doc-toggle::before { content: '▲ collapse'; }
                details:not([open]) .doc-toggle::before { content: '▼ preview'; }
                .doc-body { border-top: 1px solid #f1f5f9; }
                .doc-loading { padding: 1rem; color: #aaa; font-size: 0.78rem; }
                .doc-content { margin: 0; padding: 1rem; font-size: 0.75rem; line-height: 1.6;
                               overflow-x: auto; max-height: 320px; overflow-y: auto;
                               background: #1e293b; color: #e2e8f0; white-space: pre; }
                .doc-iframe { width: 100%; height: 300px; border: none; display: block; }
              </style>
            </head>
            <body>
            """);

        sb.AppendLine($"<h1>Workflow Run &mdash; {HE(workflowId)} v{version}</h1>");
        sb.AppendLine($"""
            <div class="meta">
              <span><strong>RunId:</strong> {HE(runId)}</span>
              <span><strong>Workflow Doc:</strong> <a href="/api/getDocument?docId={workflowDocId}">{workflowDocId}</a></span>
              <span><strong>Started:</strong> {startedAt:yyyy-MM-dd HH:mm:ss} UTC</span>
              <span><strong>Duration:</strong> {duration.TotalSeconds:F2}s</span>
            </div>
            <h2>Pipeline</h2>
            <div class="pipeline">
            """);

        // Pipeline boxes
        bool first = true;
        foreach (var stepNode in steps)
        {
            var step        = stepNode!.AsObject();
            var stepId      = step["id"]?.GetValue<string>() ?? "?";
            var stepOp      = step["operator"]?.GetValue<string>() ?? "?";
            var inTokens    = step["inputTokens"]?.AsArray()    ?? [];
            var outNames    = step["outputNames"]?.AsArray()    ?? [];
            var inResolved  = step["resolvedInputs"]?.AsArray() ?? [];
            var outResolved = step["resolvedOutputs"]?.AsArray() ?? [];

            if (!first) sb.AppendLine("  <div class=\"arrow\">&#8594;</div>");
            first = false;

            sb.AppendLine("  <div class=\"step\">");
            sb.AppendLine($"    <div class=\"step-id\">{HE(stepId)}</div>");
            sb.AppendLine($"    <div class=\"step-op\">{HE(stepOp)}</div>");

            sb.AppendLine("    <div class=\"lbl\">Inputs</div><ul>");
            for (int i = 0; i < inTokens.Count; i++)
            {
                var token = inTokens[i]?.GetValue<string>() ?? "?";
                var parts = token.Split(' ', 2);
                var name  = parts.Length > 1 ? parts[1] : token;
                var docId = i < inResolved.Count && inResolved[i] != null
                    ? (int?)inResolved[i]!.GetValue<int>() : null;

                sb.Append("      <li>");
                if (docId.HasValue)
                    sb.Append($"<span class=\"dn\"><a href=\"/api/getDocument?docId={docId}\">{HE(name)}</a></span>");
                else
                    sb.Append($"<span class=\"dn\">{HE(name)}</span>");
                sb.AppendLine($"<span class=\"di\">[{HE(parts[0])}]</span></li>");
            }
            sb.AppendLine("    </ul>");

            sb.AppendLine("    <div class=\"lbl\">Outputs</div><ul>");
            for (int i = 0; i < outNames.Count; i++)
            {
                var name  = outNames[i]?.GetValue<string>() ?? "?";
                var docId = i < outResolved.Count && outResolved[i] != null
                    ? (int?)outResolved[i]!.GetValue<int>() : null;

                sb.Append("      <li>");
                if (docId.HasValue)
                    sb.Append($"<span class=\"dn\"><a href=\"/api/getDocument?docId={docId}\">{HE(name)}</a></span>" +
                              $"<span class=\"di\">[{docId}]</span>");
                else
                    sb.Append($"<span class=\"dn\">{HE(name)}</span>");
                sb.AppendLine("</li>");
            }
            sb.AppendLine("    </ul>");
            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</div>");

        // Documents section
        sb.AppendLine("<h2>Documents</h2>");
        sb.AppendLine("<div class=\"doc-list\">");
        foreach (var (docId, docName) in docs)
        {
            sb.AppendLine($"""
                  <details id="details-{docId}">
                    <summary>
                      <span class="doc-id">{docId}</span>
                      <span class="doc-name">{HE(docName)}</span>
                      <span class="doc-toggle"></span>
                    </summary>
                    <div class="doc-body" id="body-{docId}">
                      <div class="doc-loading">Click to load&hellip;</div>
                    </div>
                  </details>
                """);
        }
        sb.AppendLine("</div>");

        // Inline JS — lazy fetch on first open
        sb.AppendLine("""
            <script>
            document.querySelectorAll('details').forEach(el => {
              el.addEventListener('toggle', async () => {
                if (!el.open) return;
                const id  = el.id.replace('details-', '');
                const body = document.getElementById('body-' + id);
                if (body.dataset.loaded) return;
                body.dataset.loaded = '1';
                try {
                  const res = await fetch('/api/getDocument?docId=' + id);
                  const ct  = (res.headers.get('content-type') || '').toLowerCase();
                  if (ct.includes('html')) {
                    body.innerHTML = `<iframe class="doc-iframe" src="/api/getDocument?docId=${id}"></iframe>`;
                  } else {
                    const text  = await res.text();
                    const lines = text.split('\n');
                    const preview = lines.slice(0, 60).join('\n') + (lines.length > 60 ? '\n… (' + lines.length + ' lines total)' : '');
                    const pre = document.createElement('pre');
                    pre.className = 'doc-content';
                    pre.textContent = preview;
                    body.innerHTML = '';
                    body.appendChild(pre);
                  }
                } catch (e) {
                  body.innerHTML = `<pre class="doc-content" style="color:#f87171">Error: ${e.message}</pre>`;
                }
              });
            });
            </script>
            </body></html>
            """);

        return sb.ToString();
    }

    private static string HE(string? s) => WebUtility.HtmlEncode(s ?? "");
}
