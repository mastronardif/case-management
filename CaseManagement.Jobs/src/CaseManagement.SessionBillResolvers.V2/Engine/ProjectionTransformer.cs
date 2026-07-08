using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CaseManagement.SessionBillResolvers.V2.Engine;
public static class ProjectionTransformer
{
    public static JsonObject Transform(
        string projectionDefinitionJson,
        string sourceJson)
    {
        var projectionDefinition =
            JsonNode.Parse(projectionDefinitionJson)!
                .AsObject();

        var source =
            JsonNode.Parse(sourceJson)!
                .AsObject();

        var result =
            new JsonObject();

        var fields =
            projectionDefinition["fields"]!
                .AsArray();

        foreach (var field in fields)
        {
            var fieldObj = field!.AsObject();
            var target   = fieldObj["target"]!.ToString();
            var type     = fieldObj.TryGetPropertyValue("type", out var typeNode) ? typeNode?.GetValue<string>() : null;

            JsonNode? value;

            if (type == "literal")
            {
                value = fieldObj["value"]?.DeepClone();
            }
            else if (type == "concat")
            {
                var separator = fieldObj.TryGetPropertyValue("separator", out var sepNode)
                    ? sepNode?.GetValue<string>() ?? ""
                    : "";
                var parts = fieldObj["source"]!.AsArray()
                    .Select(s => GetValueByPath(source, s!.GetValue<string>())?.ToString() ?? "")
                    .ToArray();
                value = JsonValue.Create(string.Join(separator, parts));
            }
            else
            {
                // default or "array" — path extraction; "array" type is semantic/documentation only
                value = GetValueByPath(source, fieldObj["source"]!.ToString());
            }

            SetByPath(result, target, value?.DeepClone());
        }

        return result;
    }

    // Navigates/creates nested objects and arrays from a dot-bracket path.
    // e.g. "loops.2400[0].DTP472.DTP03" creates { loops: { "2400": [{ DTP472: { DTP03: value } }] } }
    private static void SetByPath(JsonObject root, string path, JsonNode? value)
    {
        var segments = ParseSegments(path);
        JsonNode current = root;

        for (int i = 0; i < segments.Length - 1; i++)
        {
            var (key, idx) = segments[i];
            var obj = (JsonObject)current;

            if (idx.HasValue)
            {
                if (obj[key] is not JsonArray arr) { arr = new JsonArray(); obj[key] = arr; }
                while (arr.Count <= idx.Value) arr.Add(new JsonObject());
                current = arr[idx.Value]!;
            }
            else
            {
                if (obj[key] is not JsonObject child) { child = new JsonObject(); obj[key] = child; }
                current = child;
            }
        }

        var (lastKey, lastIdx) = segments[^1];
        var lastObj = (JsonObject)current;

        if (lastIdx.HasValue)
        {
            if (lastObj[lastKey] is not JsonArray arr) { arr = new JsonArray(); lastObj[lastKey] = arr; }
            while (arr.Count <= lastIdx.Value) arr.Add(JsonValue.Create(0)!);
            arr[lastIdx.Value] = value;
        }
        else
        {
            lastObj[lastKey] = value;
        }
    }

    private static (string Key, int? Index)[] ParseSegments(string path)
    {
        return path.Split('.').Select(part =>
        {
            var b = part.IndexOf('[');
            if (b < 0) return (part, (int?)null);
            var key = part[..b];
            var idx = int.Parse(part[(b + 1)..part.IndexOf(']')]);
            return (key, (int?)idx);
        }).ToArray();
    }

    private static JsonNode? GetValueByPath(JsonObject source, string path)
    {
        JsonNode? current = source;
        foreach (var part in path.Split('.'))
        {
            if (current is null) return null;
            var b = part.IndexOf('[');
            if (b >= 0)
            {
                var key = part[..b];
                var idx = int.Parse(part[(b + 1)..part.IndexOf(']')]);
                var arr = current[key] as JsonArray;
                current = arr is not null && idx < arr.Count ? arr[idx] : null;
            }
            else
            {
                current = current[part];
            }
        }
        return current;
    }
}

