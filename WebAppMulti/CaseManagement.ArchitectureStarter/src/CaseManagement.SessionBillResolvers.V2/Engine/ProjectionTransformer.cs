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
            var fieldObj =
                field!.AsObject();

            var target =
                fieldObj["target"]!.ToString();

            var sourcePath =
                fieldObj["source"]!.ToString();

            var value =
                GetValueByPath(
                    source,
                    sourcePath);

            result[target] =
                value?.DeepClone();
        }

        return result;
    }

    private static JsonNode? GetValueByPath(
        JsonObject source,
        string path)
    {
        JsonNode? current = source;

        var parts = path.Split('.');

        foreach (var part in parts)
        {
            current = current?[part];

            if (current == null)
            {
                return null;
            }
        }

        return current;
    }
}

