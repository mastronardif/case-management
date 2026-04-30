//public class CorqsExecutor
//{
//    private readonly DapperRepository _db;

//    public CorqsExecutor(DapperRepository db)
//    {
//        _db = db;
//    }

//    public async Task<object> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input)
//    {
//        return await _db.RunAsync(api.StoredProcedure, input);
//    }
//}
