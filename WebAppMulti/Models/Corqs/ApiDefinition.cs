namespace WebAppMulti.Models.Corqs;

public class ParamDefinition
{
    public string Type { get; set; }          // "string", "int", etc.
    public bool Required { get; set; }
    public string Source { get; set; }        // "body", "route", "query"

    // Optional (future-proofing — highly recommended)
    public string Description { get; set; }
    public string Format { get; set; }        // "uuid", "date", etc.
    public object Example { get; set; }
}

public class ApiDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "storedProcedure";
    public string? StoredProcedure { get; set; }
    public string? Sql { get; set; }
    public string? Handler { get; set; }
    public string? Url { get; set; }
    public string? Method { get; set; }
    public string? Route { get; set; }
    public Dictionary<string, ParamDefinition> Params { get; set; }
    public DataRouteDefinition? DataRoute { get; set; }

}

