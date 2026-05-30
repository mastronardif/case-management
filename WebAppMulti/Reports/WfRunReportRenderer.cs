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
        var version       = manifest["version"]?.GetValue<int>() ?? 0;
        var workflowDocId = manifest["workflowDocId"]?.GetValue<int>() ?? 0;
        var startedAt     = manifest["startedAt"]?.GetValue<DateTime>() ?? DateTime.MinValue;
        var completedAt   = manifest["completedAt"]?.GetValue<DateTime>() ?? DateTime.MinValue;
        var duration      = completedAt - startedAt;
        var steps         = manifest["steps"]?.AsArray() ?? [];

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
            <div class="pipeline">
            """);

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
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string HE(string? s) => WebUtility.HtmlEncode(s ?? "");
}
