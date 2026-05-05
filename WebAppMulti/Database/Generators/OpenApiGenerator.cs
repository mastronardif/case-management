using Newtonsoft.Json;
using System.Xml;
using WebAppMulti.Models.Corqs;
using Formatting = Newtonsoft.Json.Formatting;

public class OpenApiGenerator
{
    //public string Generate(List<ApiDefinition> apis)
    public object Generate(List<ApiDefinition> apis)
    {
        var paths = new Dictionary<string, object>();

        foreach (var api in apis)
        {
            var route = string.IsNullOrEmpty(api.Route)
                ? $"/api/{api.Name}"
                : api.Route;

            var parameters = BuildParameters(api);
            var requestBody = BuildRequestBody(api);

            paths[route] = new
            {
                post = new
                {
                    operationId = api.Name,
                    parameters,
                    requestBody,
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = new { description = "Success" }
                    }
                }
            };
        }

        var doc = new
        {
            openapi = "3.0.0",
            info = new { title = "Schema API", version = "1.0" },
            paths
        };

        return doc; // JsonConvert.SerializeObject(doc, Formatting.Indented);
    }

    private object BuildRequestBody(ApiDefinition api)
    {
        var bodyParams = api.Params?
            .Where(p => p.Value.Source == "body")
            .ToDictionary(p => p.Key, p => p.Value);

        if (bodyParams == null || bodyParams.Count == 0)
            return null;

        var required = bodyParams
            .Where(p => p.Value.Required)
            .Select(p => p.Key)
            .ToArray();

        return new
        {
            required = true,
            content = new Dictionary<string, object>
            {
                ["application/json"] = new
                {
                    schema = new
                    {
                        type = "object",
                        required,
                        properties = bodyParams.ToDictionary(
                            p => p.Key,
                            p => new { type = p.Value.Type }
                        )
                    }
                }
            }
        };
    }

    private object BuildParameters(ApiDefinition api)
    {
        if (api.Params == null) return null;

        var parameters = api.Params
            .Where(p => p.Value.Source == "route" || p.Value.Source == "query")
            .Select(p => new
            {
                name = p.Key,
                @in = p.Value.Source, // "route" or "query"
                required = p.Value.Required,
                schema = new { type = p.Value.Type }
            })
            .ToArray();

        return parameters.Length > 0 ? parameters : null;
    }

}
