using Newtonsoft.Json;
using WebAppMulti.Models.Corqs;

public class SchemaService
{
    public List<ApiDefinition> Apis { get; private set; }

    public SchemaService(IConfiguration config, IWebHostEnvironment env)
    {
        var relativePath = config["SchemaPath"] ?? "Database/Schema/schema.json";
        var fullPath = Path.Combine(env.ContentRootPath, relativePath);

        Console.WriteLine($"Loading schema from: {fullPath}");

        var json = File.ReadAllText(fullPath);

        Apis = JsonConvert.DeserializeObject<SchemaRoot>(json).Apis;
    }

}

