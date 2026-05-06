namespace WebAppMulti.Models.Corqs;

public class DataRouteDefinition
{
    public string Resource { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? IdParam { get; set; }
    public string? TypeParam { get; set; }
}
