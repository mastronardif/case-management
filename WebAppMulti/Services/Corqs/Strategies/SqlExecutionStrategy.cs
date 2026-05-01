using Dapper;
using WebAppMulti.Models.Corqs;
using WebAppMulti.Database.Repository;

namespace WebAppMulti.Services.Corqs.Strategies;

public class SqlExecutionStrategy : ICorqsExecutionStrategy
{
    private readonly DapperRepository _db;

    public string Type => "sql";

    public SqlExecutionStrategy(DapperRepository db)
    {
        _db = db;
    }

    public async Task<object> ExecuteAsync(
        ApiDefinition api,
        Dictionary<string, object?> input)
    {
        if (string.IsNullOrWhiteSpace(api.Sql))
            throw new InvalidOperationException(
                $"SQL API '{api.Name}' is missing Sql text.");

        return await _db.QueryAsync(
            api.Sql,
            input,
            commandType: System.Data.CommandType.Text);
    }
}