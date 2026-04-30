using WebAppMulti.Database.Repository;
using WebAppMulti.Models.Corqs;

public class StoredProcedureStrategy : ICorqsExecutionStrategy
{
    public string Type => "storedProcedure";

    private readonly DapperRepository _repository;

    public StoredProcedureStrategy(DapperRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input)
    {
        return await _repository.ExecuteStoredProcedureAsync(
            api.StoredProcedure!,
            input);
    }
}