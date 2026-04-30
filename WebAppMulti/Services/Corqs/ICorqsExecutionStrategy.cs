using WebAppMulti.Models.Corqs;

public interface ICorqsExecutionStrategy
{
    string Type { get; }
    Task<object> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input);
}