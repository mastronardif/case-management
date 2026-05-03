namespace WebAppMulti.Models.Corqs;

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
}

//public class ApiDefinition
//{
//    public string Name { get; set; } = string.Empty;
//    public string Type { get; set; } = string.Empty;
//    public string? StoredProcedure { get; set; }
//    public string? Sql { get; set; }
//    public string? Handler { get; set; }
//    public string? Url { get; set; }
//    public string Route { get; set; } = string.Empty;
//}

