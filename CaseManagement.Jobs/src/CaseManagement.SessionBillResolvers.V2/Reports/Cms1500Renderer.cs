using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace CaseManagement.SessionBillResolvers.V2.Reports;

public static class Cms1500Renderer
{
    public static string Render(string boxesJson)
    {
        var root  = JsonNode.Parse(boxesJson)!.AsObject();
        var boxes = root["boxes"]?.AsObject() ?? new JsonObject();

        string B(string key) => WebUtility.HtmlEncode(boxes[key]?.ToString() ?? "");

        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <title>CMS-1500 Claim Review</title>
              <style>
                *, *::before, *::after { box-sizing: border-box; }
                body { font-family: Consolas, monospace; background: #f5f5f5; margin: 20px; color: #222; }
                h2 { font-size: 1.1rem; margin-bottom: 1rem; }
                h3 { font-size: 0.9rem; margin: 1.5rem 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em; color: #555; }
                table { width: 100%; border-collapse: collapse; background: #fff; margin-bottom: 1rem; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
                th { background: #f0f0f0; text-align: left; padding: 8px 12px; font-size: 0.8rem; font-weight: bold; width: 18%; border-bottom: 1px solid #ddd; }
                td { padding: 8px 12px; font-size: 0.85rem; border-bottom: 1px solid #f0f0f0; }
                tr:last-child td, tr:last-child th { border-bottom: none; }
                thead th { background: #1e293b; color: #e2e8f0; font-size: 0.78rem; }
                tbody tr:nth-child(even) td { background: #f8fafc; }
              </style>
            </head>
            <body>
            <h2>CMS-1500 Claim Review</h2>
            """);

        // Main boxes
        sb.AppendLine("""<table><tbody>""");
        MainRow(sb, "Box 1",  B("box1"),  "Box 1a", B("box1a"));
        MainRow(sb, "Box 2",  B("box2"),  "Box 3",  B("box3"));
        MainRow(sb, "Box 4",  B("box4"),  "Box 21", BoxArray(boxes, "box21"));
        MainRow(sb, "Box 23", B("box23"), "Box 25", B("box25"));
        MainRow(sb, "Box 28", B("box28"), "Box 31", B("box31"));
        sb.AppendLine("""</tbody></table>""");

        // Box 24 — Service Lines
        sb.AppendLine("""
            <h3>Box 24 — Service Lines</h3>
            <table>
              <thead><tr><th>#</th><th>Date of Service (24A)</th><th>CPT (24D)</th><th>Modifier</th></tr></thead>
              <tbody>
            """);

        var lines = boxes["box24"] as JsonArray ?? new JsonArray();
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i]?.AsObject();
            var date = line?["DTP472"]?["DTP03"]?.ToString() ?? "";
            var cpt  = line?["SV1"]?["SV101_2"]?.ToString()  ?? "";
            var mod  = line?["SV1"]?["SV101_3"]?.ToString()  ?? "";
            sb.AppendLine($"""
                <tr>
                  <td>{i + 1}</td>
                  <td>{WebUtility.HtmlEncode(date)}</td>
                  <td>{WebUtility.HtmlEncode(cpt)}</td>
                  <td>{WebUtility.HtmlEncode(mod)}</td>
                </tr>
                """);
        }

        sb.AppendLine("""</tbody></table>""");

        // Box 33 — Billing Provider
        var box33 = boxes["box33"]?.AsObject();
        sb.AppendLine("""
            <h3>Billing Provider (Box 33)</h3>
            <table><tbody>
            """);

        ProviderRow(sb, "Name",     box33?["name"]?.ToString());
        ProviderRow(sb, "Address",  box33?["address1"]?.ToString());
        ProviderRow(sb, "City",     box33?["city"]?.ToString());
        ProviderRow(sb, "State",    box33?["state"]?.ToString());
        ProviderRow(sb, "Zip",      box33?["zip"]?.ToString());
        ProviderRow(sb, "NPI (33a)", B("box33a"));

        sb.AppendLine("</tbody></table></body></html>");

        return sb.ToString();
    }

    private static void MainRow(StringBuilder sb, string lbl1, string val1, string lbl2, string val2) =>
        sb.AppendLine($"<tr><th>{lbl1}</th><td>{val1}</td><th>{lbl2}</th><td>{val2}</td></tr>");

    private static void ProviderRow(StringBuilder sb, string label, string? value) =>
        sb.AppendLine($"<tr><th>{WebUtility.HtmlEncode(label)}</th><td>{WebUtility.HtmlEncode(value ?? "")}</td></tr>");

    private static string BoxArray(JsonObject boxes, string key)
    {
        var arr = boxes[key] as JsonArray;
        if (arr is null || arr.Count == 0) return "";
        return WebUtility.HtmlEncode(string.Join(", ", arr.Select(e => e?.ToString() ?? "")));
    }
}
