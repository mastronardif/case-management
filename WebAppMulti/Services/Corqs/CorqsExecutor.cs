using WebAppMulti.Models.Corqs;

public class CorqsExecutor
{
    private readonly Dictionary<string, ICorqsExecutionStrategy> _strategies;

    public CorqsExecutor(IEnumerable<ICorqsExecutionStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            s => s.Type,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<object> ExecuteAsync(
    ApiDefinition api,
    Dictionary<string, object?> input)
    {
        var result = await _strategies[api.Type]
            .ExecuteAsync(api, input);

        return new CorqsResponse
        {
            Success = true,
            Data = result
        };
    }

    //public async Task<object> ExecuteAsync(
    //    ApiDefinition api,
    //    Dictionary<string, object?> input)
    //{
    //    if (!_strategies.TryGetValue(api.Type, out var strategy))
    //    {
    //        throw new InvalidOperationException(
    //            $"Unsupported API type '{api.Type}'.");
    //    }

    //    return await strategy.ExecuteAsync(api, input);
    //}
}
