using Newtonsoft.Json;
using System.Xml;
using WebAppMulti.Models.Corqs;
using Formatting = Newtonsoft.Json.Formatting;

public class PostmanGenerator
{
    public object Generate(List<ApiDefinition> apis)
    {
        return new
        {
            info = new
            {
                name = "Schema API",
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            item = apis.Select(api => new
            {
                name = api.Name,
                request = new
                {
                    method = "POST",
                    url = $"http://localhost:5000{api.Route}",
                    header = new[]
                    {
                        new { key = "Content-Type", value = "application/json" }
                    },
                    body = new
                    {
                        mode = "raw",
                        raw = BuildExampleBody(api)
                    }
                }
            })
        };
    }

    private string BuildExampleBody(ApiDefinition api)
    {
        if (api.Params == null || !api.Params.Any())
            return "{}";

        var obj = api.Params.ToDictionary(
            p => p.Key,
            p => GetExampleValue(p.Value.Type)
        );

        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }

    private object GetExampleValue(string type)
    {
        return type switch
        {
            "string" => "example",
            "int" => 0,
            "bool" => true,
            _ => null
        };
    }

}
