using WebAppMulti.Database.Repository;

public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly DapperRepository _repo;

    public StoredProcedureExecutor(DapperRepository repo)
    {
        _repo = repo;
    }

    public async Task<object> ExecuteAsync(
        string storedProcedure,
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        return await _repo.ExecuteStoredProcedureAsync(
            storedProcedure,
            parameters);
    }
}
