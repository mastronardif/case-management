using WebAppMulti.Database.Repository;
using WebAppMulti.Models.Corqs;

public class CorqsExecutor
{
    private readonly DapperRepository _db;

    public CorqsExecutor(DapperRepository db)
    {
        _db = db;
    }

    public Task<object> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input)
    {
        return _db.RunAsync(api.StoredProcedure, input);
    }
}
