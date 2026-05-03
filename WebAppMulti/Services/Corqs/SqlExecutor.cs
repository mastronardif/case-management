using System.Data;
using WebAppMulti.Database.Repository;
using WebAppMulti.Services.Corqs;

public class SqlExecutor : ISqlExecutor
{
    private readonly DapperRepository _repo;

    public SqlExecutor(DapperRepository repo)
    {
        _repo = repo;
    }

    public async Task<object> ExecuteAsync(
        string sql,
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        return await _repo.QueryAsync(sql, parameters);
    }
}
