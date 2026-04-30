public interface ICorqsHandler
{
    string Name { get; }
    Task<object> ExecuteAsync(Dictionary<string, object?> input);
}