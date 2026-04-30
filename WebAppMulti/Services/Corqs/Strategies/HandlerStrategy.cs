using WebAppMulti.Models.Corqs;

public class HandlerStrategy : ICorqsExecutionStrategy
{
    public string Type => "handler";

    private readonly IEnumerable<ICorqsHandler> _handlers;

    public HandlerStrategy(IEnumerable<ICorqsHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task<object> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input)
    {
        var handler = _handlers.FirstOrDefault(h =>
            h.Name.Equals(api.Handler, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
            throw new InvalidOperationException(
                $"Handler '{api.Handler}' not found.");

        return await handler.ExecuteAsync(input);
    }
}