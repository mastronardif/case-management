using WebAppMulti.Database.Repository;
using WebAppMulti.Models.Corqs;

// Stored procedures that return several result sets in one call. api.ResultSets declares the
// names, in the same order the SP's SELECT statements execute — result set N maps to
// ResultSets[N]. Trailing names are simply absent from the response if the SP returns fewer
// result sets than declared (e.g. while a procedure is still being filled in).
public class StoredProcedureMultiStrategy : ICorqsExecutionStrategy
{
    public string Type => "storedProcedureMulti";

    private readonly DapperRepository _repository;

    public StoredProcedureMultiStrategy(DapperRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input)
    {
        if (api.ResultSets is null || api.ResultSets.Length == 0)
            throw new InvalidOperationException($"API '{api.Name}' is type 'storedProcedureMulti' but declares no resultSets.");

        return await _repository.RunNamedMultiResultAsync(
            api.StoredProcedure!,
            input,
            api.ResultSets);
    }
}
