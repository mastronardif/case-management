using WebAppMulti.Models.Corqs;

namespace WebAppMulti.Services.Corqs;

public interface IHttpProxyExecutor
{
    Task<object?> ExecuteAsync(
        ApiDefinition api,
        IDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
