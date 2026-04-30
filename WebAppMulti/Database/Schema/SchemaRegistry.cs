using System.Text.Json;
using WebAppMulti.Models.Corqs;
public class SchemaRegistry
{
    private readonly SchemaDefinition _schema;

    public SchemaRegistry(IWebHostEnvironment env)
    {
        var path = Path.Combine(
            env.ContentRootPath,
            "Database",
            "Schema",
            "schema.json");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Schema not found at {path}");

        var json = File.ReadAllText(path);

        _schema = JsonSerializer.Deserialize<SchemaDefinition>(
    json,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    })
    ?? throw new Exception("Schema failed to load");
    }

    public ApiDefinition? GetApi(string name)
    {
        Console.WriteLine($"Loaded APIs: {_schema.Apis.Count}");

        return _schema.Apis
            .FirstOrDefault(x =>
                x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void WarmUp()
    {
        Console.WriteLine($"APIs loaded: {_schema.Apis.Count}");
    }
}

