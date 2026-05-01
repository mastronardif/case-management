public class GetBookHandler : ICorqsHandler
{
    private readonly IWebHostEnvironment _env;
    public string Name => "getBook";



    public GetBookHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Task<object> ExecuteAsync(Dictionary<string, object?> input)
    {
        var caseId = input.GetValueOrDefault("caseId")?.ToString();
        caseId = "42";
        var fileName = input.GetValueOrDefault("fileName")?.ToString();

        if (string.IsNullOrWhiteSpace(caseId) || string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("caseId and fileName are required.");

        var filePath = Path.Combine(
            _env.ContentRootPath,
            "Workbooks",
            caseId,
            fileName);

        if (!File.Exists(filePath))
            return Task.FromResult<object>(new
            {
                found = false
            });

        return Task.FromResult<object>(new
        {
            found = true,
            fileName,
            downloadUrl = $"/api/workbooks/GetBook?caseId={Uri.EscapeDataString(caseId)}&fileName={Uri.EscapeDataString(fileName)}"
        });
    }
}
