using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Engine;

public record MappedField(string Target, string Source, string? ExtractedValue);
public record ValidationIssue(string Target, string Source, string Reason);
public record ProjectionResult(
    IReadOnlyList<MappedField> MappedFields,
    IReadOnlyList<ValidationIssue> ValidationIssues);

public record AuditFieldRow(
    string Target,
    string Source,
    string? ExtractedValue,
    string Status);

public record AuditDocument(
    string RunId,
    string RunAction,
    DateTime CreatedDate,
    IReadOnlyList<AuditFieldRow> Fields);

public class ProjectProcessor(ICaseManagementRepository repository, ILogger<ProjectProcessor> logger)
{
    public async Task RunAsync(RunInput input, string outputDirectory, CancellationToken ct)
    {
        logger.LogInformation("projectorProcess started. RunId: {RunId}", input.RunId);

        var sessionDoc = await repository.GetDocumentAsync(new DocumentContext(DocumentId: input.InputDoc), ct);
        var ruleDoc    = await repository.GetDocumentAsync(new DocumentContext(DocumentId: input.ProjectionDefinitionId), ct);

        if (sessionDoc is null) { logger.LogError("Session doc {Id} not found", input.InputDoc);              return; }
        if (ruleDoc is null)    { logger.LogError("Rule doc {Id} not found",    input.ProjectionDefinitionId); return; }

        var result     = Project(sessionDoc.Content, ruleDoc.Content);
        var auditJson  = RenderAuditJson(input, result);
        var reviewHtml = RenderReviewHtml(sessionDoc.Content, ruleDoc.Content, result);

        var auditPath  = Path.Combine(outputDirectory, $"{input.RunId}.audit.json");
        var reviewPath = Path.Combine(outputDirectory, $"{input.RunId}.review.html");

        await File.WriteAllTextAsync(auditPath,  auditJson,  ct);
        await File.WriteAllTextAsync(reviewPath, reviewHtml, ct);

        logger.LogInformation(
            "projectorProcess complete. Fields: {Total}, Issues: {Issues}. Files: {AuditPath}, {ReviewPath}",
            result.MappedFields.Count, result.ValidationIssues.Count, auditPath, reviewPath);
    }

    // Resolves all rule fields against the session document.
    public ProjectionResult Project(string sessionJson, string ruleJson)
    {
        var session = JsonNode.Parse(sessionJson)!.AsObject();
        var fields = JsonNode.Parse(ruleJson)!.AsObject()["fields"]!.AsArray();

        var mapped = new List<MappedField>();
        var issues = new List<ValidationIssue>();

        foreach (var field in fields)
        {
            var f = field!.AsObject();
            var target = f["target"]!.ToString();
            var source = f["source"]!.ToString();
            var value = GetValueByPath(session, source)?.ToString();

            mapped.Add(new MappedField(target, source, value));

            if (string.IsNullOrWhiteSpace(value))
                issues.Add(new ValidationIssue(target, source, "Value is null or missing"));
        }

        return new ProjectionResult(mapped, issues);
    }

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Produces a 4-column JSON audit file: target | source | extractedValue | status.
    // status is "ok", "missing" (null/absent), or "empty" (blank string).
    public static string RenderAuditJson(RunInput run, ProjectionResult result)
    {
        var rows = result.MappedFields.Select(f =>
        {
            var status = f.ExtractedValue is null      ? "missing"
                       : f.ExtractedValue.Trim() == "" ? "empty"
                                                       : "ok";
            return new AuditFieldRow(f.Target, f.Source, f.ExtractedValue, status);
        }).ToList();

        var doc = new AuditDocument(run.RunId, run.RunAction, run.CreatedDate, rows);

        return JsonSerializer.Serialize(doc, AuditJsonOptions);
    }

    // Produces a 4-column HTML review doc: Field | Extracted Value | Rule | Corrected Value.
    // Mapped (rule-referenced) fields are highlighted; validation failures appear in a summary banner.
    public string RenderReviewHtml(string sessionJson, string ruleJson, ProjectionResult result)
    {
        var session = JsonNode.Parse(sessionJson)!.AsObject();

        // source path → display label shown in the Rule column
        var ruleMap = result.MappedFields.ToDictionary(
            f => f.Source,
            f => $"{f.Source} → {f.Target}");

        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("<h1>Session Projection Review</h1>");

        if (result.ValidationIssues.Count > 0)
        {
            sb.AppendLine("<div class=\"validation-errors\">");
            sb.AppendLine($"<h2>Validation Issues ({result.ValidationIssues.Count})</h2><ul>");
            foreach (var issue in result.ValidationIssues)
                sb.AppendLine($"<li><strong>{E(issue.Target)}</strong> ({E(issue.Source)}): {E(issue.Reason)}</li>");
            sb.AppendLine("</ul></div>");
        }

        // Top-level scalar fields (team, clientLegalName, etc.) go in a Root section
        var rootLeaves = session.Where(p => IsLeaf(p.Value)).ToList();
        if (rootLeaves.Count > 0)
        {
            OpenSection(sb, "Root");
            OpenTable(sb);
            foreach (var p in rootLeaves)
                AppendRow(sb, p.Key, p.Key, p.Value?.ToString(), ruleMap);
            CloseTable(sb);
            CloseSection(sb);
        }

        foreach (var p in session.Where(p => !IsLeaf(p.Value)))
            RenderNode(sb, p.Key, p.Value!, p.Key, ruleMap);

        AppendFooter(sb);
        return sb.ToString();
    }

    private static void RenderNode(StringBuilder sb, string name, JsonNode node, string path, Dictionary<string, string> ruleMap)
    {
        if (node is JsonObject obj)
            RenderObject(sb, name, obj, path, ruleMap);
        else if (node is JsonArray arr)
            RenderArray(sb, name, arr, path, ruleMap);
    }

    private static void RenderObject(StringBuilder sb, string name, JsonObject obj, string path, Dictionary<string, string> ruleMap)
    {
        OpenSection(sb, name);

        var leaves = obj.Where(p => IsLeaf(p.Value)).ToList();
        if (leaves.Count > 0)
        {
            OpenTable(sb);
            foreach (var p in leaves)
                AppendRow(sb, p.Key, $"{path}.{p.Key}", p.Value?.ToString(), ruleMap);
            CloseTable(sb);
        }

        foreach (var p in obj.Where(p => !IsLeaf(p.Value)))
            RenderNode(sb, p.Key, p.Value!, $"{path}.{p.Key}", ruleMap);

        CloseSection(sb);
    }

    private static void RenderArray(StringBuilder sb, string name, JsonArray arr, string basePath, Dictionary<string, string> ruleMap)
    {
        OpenSection(sb, name);
        sb.AppendLine("<div class=\"nested\">");

        for (int i = 0; i < arr.Count; i++)
        {
            var item = arr[i];
            var path = $"{basePath}[{i}]";

            if (item is JsonObject or JsonArray)
            {
                RenderNode(sb, $"{name} [{i}]", item!, path, ruleMap);
            }
            else
            {
                OpenSection(sb, $"{name} [{i}]");
                OpenTable(sb);
                AppendRow(sb, $"{name} [{i}]", path, item?.ToString(), ruleMap);
                CloseTable(sb);
                CloseSection(sb);
            }
        }

        sb.AppendLine("</div>");
        CloseSection(sb);
    }

    private static void AppendRow(StringBuilder sb, string field, string path, string? value, Dictionary<string, string> ruleMap)
    {
        ruleMap.TryGetValue(path, out var rule);
        var rowClass = rule is not null ? " class=\"mapped\"" : "";
        var displayValue = E(value ?? "");
        var ruleCell = rule is not null
            ? E(rule)
            : "<span class=\"not-mapped\">—</span>";

        sb.AppendLine($"<tr{rowClass}>");
        sb.AppendLine($"  <td>{E(field)}</td>");
        sb.AppendLine($"  <td>{displayValue}</td>");
        sb.AppendLine($"  <td>{ruleCell}</td>");
        sb.AppendLine($"  <td><input type=\"text\" value=\"{displayValue}\"></td>");
        sb.AppendLine("</tr>");
    }

    private static bool IsLeaf(JsonNode? node) => node is not JsonObject and not JsonArray;

    private static void OpenSection(StringBuilder sb, string title) =>
        sb.AppendLine($"<div class=\"section\"><h2>{E(title)}</h2>");

    private static void CloseSection(StringBuilder sb) => sb.AppendLine("</div>");

    private static void OpenTable(StringBuilder sb)
    {
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Field</th><th>Extracted Value</th><th>Rule</th><th>Corrected Value</th></tr>");
    }

    private static void CloseTable(StringBuilder sb) => sb.AppendLine("</table>");

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static JsonNode? GetValueByPath(JsonObject source, string path)
    {
        JsonNode? current = source;
        foreach (var part in path.Split('.'))
        {
            current = current?[part];
            if (current == null) return null;
        }
        return current;
    }

    private static void AppendHeader(StringBuilder sb) => sb.AppendLine("""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <title>Projection Review</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
                h1 { margin-bottom: 30px; }
                .section { background: white; border-radius: 8px; padding: 16px; margin-bottom: 24px; box-shadow: 0 2px 6px rgba(0,0,0,0.1); }
                .section h2 { margin-top: 0; border-bottom: 2px solid #ddd; padding-bottom: 8px; }
                .nested { margin-left: 20px; margin-top: 10px; }
                table { width: 100%; border-collapse: collapse; }
                th { text-align: left; background: #f0f0f0; padding: 10px; }
                td { padding: 10px; border-bottom: 1px solid #ddd; vertical-align: top; }
                tr.mapped td { background: #f0fff0; }
                tr.mapped td:nth-child(3) { color: #2a7a2a; font-size: 0.85em; font-family: monospace; }
                .not-mapped { color: #bbb; }
                input[type="text"] { width: 100%; padding: 6px; box-sizing: border-box; }
                .validation-errors { background: #fff3f3; border: 1px solid #f5c6cb; border-radius: 8px; padding: 16px; margin-bottom: 24px; }
                .validation-errors h2 { color: #c0392b; margin-top: 0; }
            </style>
        </head>
        <body>
        """);

    private static void AppendFooter(StringBuilder sb) => sb.AppendLine("</body></html>");
}
