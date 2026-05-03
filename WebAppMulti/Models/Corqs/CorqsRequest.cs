public class CorqsRequest
{
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> Params { get; set; } = new();
}
