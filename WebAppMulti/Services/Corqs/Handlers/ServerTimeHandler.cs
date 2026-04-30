public class ServerTimeHandler : ICorqsHandler
{
    public string Name => "ServerTime";

    public Task<object> ExecuteAsync(Dictionary<string, object?> input)
    {
        return Task.FromResult<object>(new
        {
            serverTime = DateTime.UtcNow,
            timeZone = "UTC"
        });
    }
}