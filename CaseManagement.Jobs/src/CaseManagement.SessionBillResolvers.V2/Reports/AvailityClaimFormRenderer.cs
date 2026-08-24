using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace CaseManagement.SessionBillResolvers.V2.Reports;

public static class AvailityClaimFormRenderer
{
    public static string Render(string previewJson)
    {
        var root = JsonNode.Parse(previewJson)!.AsObject();

        string S(JsonObject? obj, string key) => WebUtility.HtmlEncode(obj?[key]?.ToString() ?? "");
        string R(string key) => S(root, key);

        var subscriber = root["subscriber"]?.AsObject();
        var patient    = root["patient"]?.AsObject();
        var billing    = root["billingProvider"]?.AsObject();
        var rendering  = root["renderingProvider"]?.AsObject();
        var lines      = root["serviceLines"] as JsonArray ?? new JsonArray();

        var isSelf = patient is null || (patient["lastName"] is null && patient["firstName"] is null);

        var sb = new StringBuilder();
        sb.AppendLine("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <title>Availity Claim Form Preview</title>
              <style>
                *, *::before, *::after { box-sizing: border-box; }
                body { font-family: Consolas, monospace; background: #f5f5f5; margin: 20px; color: #222; }
                h2 { font-size: 1.1rem; margin-bottom: 1rem; }
                h3 { font-size: 0.9rem; margin: 1.5rem 0 0.5rem; text-transform: uppercase; letter-spacing: 0.05em; color: #555; }
                table { width: 100%; border-collapse: collapse; background: #fff; margin-bottom: 1rem; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
                th { background: #f0f0f0; text-align: left; padding: 8px 12px; font-size: 0.8rem; font-weight: bold; width: 18%; border-bottom: 1px solid #ddd; }
                td { padding: 8px 12px; font-size: 0.85rem; border-bottom: 1px solid #f0f0f0; }
                tr:last-child td, tr:last-child th { border-bottom: none; }
                thead th { background: #1e293b; color: #e2e8f0; font-size: 0.78rem; width: auto; }
                tbody tr:nth-child(even) td { background: #f8fafc; }
                td.num { text-align: right; }
                tr.total td { font-weight: bold; border-top: 2px solid #333; background: #fff !important; }
                .badge { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 0.72rem; }
                .badge-ok   { background: #f0fdf4; color: #166534; border: 1px solid #86efac; }
                .badge-warn { background: #fef2f2; color: #991b1b; border: 1px solid #fca5a5; }
                .muted { color: #999; }
              </style>
            </head>
            <body>
            <h2>Availity Claim Form Preview</h2>
            """);

        // Header — Organization / Claim Type / Payer / Responsibility
        sb.AppendLine("<table><tbody>");
        MainRow(sb, "Organization", R("organization"), "Claim Type", R("claimType"));
        MainRow(sb, "Payer", R("payer"), "Responsibility Sequence", R("responsibilitySequence"));
        MainRow(sb, "Claim Filing Indicator", R("claimFilingIndicator"), "Place of Service", R("placeOfService"));
        MainRow(sb, "Patient Control Number", R("patientControlNumber"), "Prior Authorization Number", R("priorAuthorizationNumber"));
        MainRow(sb, "Principal Diagnosis Code", R("principalDiagnosisCode"), "Group Number", R("groupNumber"));
        sb.AppendLine("</tbody></table>");

        // Subscriber
        sb.AppendLine("<h3>Subscriber / Insured</h3><table><tbody>");
        MainRow(sb, "Last Name", S(subscriber, "lastName"), "First Name", S(subscriber, "firstName"));
        MainRow(sb, "Date of Birth", S(subscriber, "dateOfBirth"), "Gender", S(subscriber, "gender"));
        MainRow(sb, "Subscriber / Insured ID", S(subscriber, "memberId"), "Relationship", R("relationshipCode"));
        sb.AppendLine("</tbody></table>");

        // Patient — only when different from subscriber (mirrors Availity's own form behavior)
        sb.AppendLine("<h3>Patient</h3>");
        if (isSelf)
        {
            sb.AppendLine("<p class=\"muted\">Relationship = Self &mdash; patient is the subscriber.</p>");
        }
        else
        {
            sb.AppendLine("<table><tbody>");
            MainRow(sb, "Last Name", S(patient, "lastName"), "First Name", S(patient, "firstName"));
            MainRow(sb, "Date of Birth", S(patient, "dateOfBirth"), "Gender", S(patient, "gender"));
            sb.AppendLine("</tbody></table>");
        }

        // Billing Provider
        sb.AppendLine("<h3>Billing Provider</h3><table><tbody>");
        MainRow(sb, "Name", S(billing, "name"), "NPI", S(billing, "npi"));
        MainRow(sb, "EIN", S(billing, "ein"), "Address", S(billing, "address1"));
        MainRow(sb, "City", S(billing, "city"), "State / Zip", $"{S(billing, "state")} {S(billing, "zip")}".Trim());
        sb.AppendLine("</tbody></table>");

        // Rendering Provider (claim-level primary — 2310B)
        sb.AppendLine("<h3>Rendering Provider (Primary)</h3><table><tbody>");
        var renderingName = $"{S(rendering, "firstName")} {S(rendering, "lastName")}".Trim();
        MainRow(sb, "Name", renderingName, "NPI", S(rendering, "npi"));
        sb.AppendLine("</tbody></table>");

        // Service Lines
        sb.AppendLine("""
            <h3>Service Lines</h3>
            <table>
              <thead><tr>
                <th>DOS</th><th>CPT</th><th>Modifier</th><th>Rendering Provider</th>
                <th class="num">Minutes</th><th class="num">Units</th><th class="num">Rate</th><th class="num">Charge</th>
              </tr></thead>
              <tbody>
            """);

        decimal lineSum = 0m;
        foreach (var node in lines)
        {
            var line = node as JsonObject;
            var dos     = FormatDos(line?["DTP472"]?["DTP03"]?.ToString());
            var cpt     = line?["SV1"]?["SV101_2"]?.ToString() ?? "";
            var mod     = line?["SV1"]?["SV101_3"]?.ToString() ?? "";
            var minutes = line?["computed"]?["minutes"]?.ToString() ?? "";
            var unitsN  = line?["SV1"]?["SV104"]?.GetValue<decimal>() ?? 0m;
            var chargeN = line?["SV1"]?["SV102"]?.GetValue<decimal>() ?? 0m;
            var rateN   = unitsN != 0 ? chargeN / unitsN : 0m;
            lineSum += chargeN;

            var overrideLast  = line?["NM1_82"]?["NM103"]?.ToString();
            var overrideFirst = line?["NM1_82"]?["NM104"]?.ToString();
            var lineProviderHtml = !string.IsNullOrWhiteSpace(overrideLast)
                ? HE($"{overrideFirst} {overrideLast}".Trim())
                : $"<span class=\"muted\">{HE(renderingName)} (primary)</span>";

            sb.AppendLine($"""
                <tr>
                  <td>{HE(dos)}</td>
                  <td>{HE(cpt)}</td>
                  <td>{HE(mod)}</td>
                  <td>{lineProviderHtml}</td>
                  <td class="num">{HE(minutes)}</td>
                  <td class="num">{unitsN}</td>
                  <td class="num">{rateN:C}</td>
                  <td class="num">{chargeN:C}</td>
                </tr>
                """);
        }

        var totalCharge = root["totalCharge"]?.GetValue<decimal>() ?? 0m;
        var matches = lineSum == totalCharge;
        sb.AppendLine($"""
            <tr class="total">
              <td colspan="7">TOTAL <span class="badge {(matches ? "badge-ok" : "badge-warn")}">{(matches ? "✓ matches line sum" : $"⚠ line sum is {lineSum:C}")}</span></td>
              <td class="num">{totalCharge:C}</td>
            </tr>
            """);

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private static string FormatDos(string? yyyyMMdd) =>
        !string.IsNullOrEmpty(yyyyMMdd) && yyyyMMdd.Length == 8
            ? $"{yyyyMMdd[4..6]}/{yyyyMMdd[6..8]}"
            : yyyyMMdd ?? "";

    private static void MainRow(StringBuilder sb, string lbl1, string val1, string lbl2, string val2) =>
        sb.AppendLine($"<tr><th>{HE(lbl1)}</th><td>{val1}</td><th>{HE(lbl2)}</th><td>{val2}</td></tr>");

    private static string HE(string? s) => WebUtility.HtmlEncode(s ?? "");
}
