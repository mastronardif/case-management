using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace CaseManagement.Common;

public static class JsonToHtmlConverter
{
    public static string Convert(string json)
    {
        using var doc = JsonDocument.Parse(json);

        var sb = new StringBuilder();

        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>JSON Review</title>

    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 20px;
            background: #f5f5f5;
        }

        h1 {
            margin-bottom: 30px;
        }

        .section {
            background: white;
            border-radius: 8px;
            padding: 16px;
            margin-bottom: 24px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.1);
        }

        .section h2 {
            margin-top: 0;
            border-bottom: 2px solid #ddd;
            padding-bottom: 8px;
        }

        table {
            width: 100%;
            border-collapse: collapse;
        }

        th {
            text-align: left;
            background: #f0f0f0;
            padding: 10px;
        }

        td {
            padding: 10px;
            border-bottom: 1px solid #ddd;
            vertical-align: top;
        }

        input[type="text"],
        textarea {
            width: 100%;
            padding: 6px;
            box-sizing: border-box;
        }

        textarea {
            min-height: 80px;
            resize: vertical;
        }

        ul {
            margin: 0;
            padding-left: 20px;
        }

        .nested {
            margin-left: 20px;
            margin-top: 10px;
        }
    </style>
</head>
<body>

<h1>JSON Extraction Review</h1>
""");

        RenderElement(sb, doc.RootElement, "Root");

        sb.AppendLine("""
</body>
</html>
""");

        return sb.ToString();
    }

    private static void RenderElement(
        StringBuilder sb,
        JsonElement element,
        string sectionName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                RenderObject(sb, element, sectionName);
                break;

            case JsonValueKind.Array:
                RenderArray(sb, element, sectionName);
                break;

            default:
                RenderPrimitive(sb, sectionName, element.ToString());
                break;
        }
    }

    private static void RenderObject(
        StringBuilder sb,
        JsonElement obj,
        string title)
    {
        sb.AppendLine($"""
<div class="section">
    <h2>{Html(title)}</h2>

    <table>
        <tr>
            <th>Field</th>
            <th>Extracted Value</th>
            <th>Corrected Value</th>
        </tr>
""");

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object ||
                prop.Value.ValueKind == JsonValueKind.Array)
            {
                continue;
            }

            var value = prop.Value.ToString();

            sb.AppendLine($"""
        <tr>
            <td>{Html(prop.Name)}</td>
            <td>{Html(value)}</td>
            <td>
                <input type="text" value="{HtmlAttribute(value)}">
            </td>
        </tr>
""");
        }

        sb.AppendLine("""
    </table>
</div>
""");

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object ||
                prop.Value.ValueKind == JsonValueKind.Array)
            {
                RenderElement(sb, prop.Value, prop.Name);
            }
        }
    }

    private static void RenderArray(
        StringBuilder sb,
        JsonElement array,
        string title)
    {
        sb.AppendLine($"""
<div class="section">
    <h2>{Html(title)}</h2>
""");

        int i = 0;

        foreach (var item in array.EnumerateArray())
        {
            sb.AppendLine($"""
<div class="nested">
""");

            RenderElement(sb, item, $"{title} [{i}]");

            sb.AppendLine("""
</div>
""");

            i++;
        }

        sb.AppendLine("""
</div>
""");
    }

    private static void RenderPrimitive(
        StringBuilder sb,
        string name,
        string? value)
    {
        value ??= "";

        sb.AppendLine($"""
<div class="section">
    <h2>{Html(name)}</h2>

    <table>
        <tr>
            <th>Field</th>
            <th>Extracted Value</th>
            <th>Corrected Value</th>
        </tr>

        <tr>
            <td>{Html(name)}</td>
            <td>{Html(value)}</td>
            <td>
                <input type="text" value="{HtmlAttribute(value)}">
            </td>
        </tr>
    </table>
</div>
""");
    }

    private static string Html(string? value)
    {
        return System.Net.WebUtility.HtmlEncode(value ?? "");
    }

    private static string HtmlAttribute(string? value)
    {
        return Html(value)
            .Replace("\"", "&quot;");
    }
}
