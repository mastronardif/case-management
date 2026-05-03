namespace WebAppMulti.Services.Corqs;

public interface ISqlExecutor
{
    Task<object> ExecuteAsync(
        string sql,
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
