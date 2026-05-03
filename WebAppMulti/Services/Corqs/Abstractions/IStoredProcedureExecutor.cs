public interface IStoredProcedureExecutor
{
    Task<object> ExecuteAsync(
        string storedProcedure,
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
