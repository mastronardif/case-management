using WebAppMulti.Models.Corqs;

namespace WebAppMulti.Services.Corqs;

public interface IRequestContextBuilder
{
    Task<IDictionary<string, object?>> BuildAsync(
        HttpContext context,
        ApiDefinition api);
}
