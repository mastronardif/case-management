public class RequestContext
{
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new();
}
