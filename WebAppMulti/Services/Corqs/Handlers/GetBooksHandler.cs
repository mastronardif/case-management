
public class GetBooksHandler : ICorqsHandler
{
    private readonly IWebHostEnvironment _env;
    public string Name => "getBooks";


    public GetBooksHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Task<object> ExecuteAsync(Dictionary<string, object?> input)
    {
        var caseId = input.GetValueOrDefault("caseId")?.ToString();
        caseId = "42";

        if (string.IsNullOrWhiteSpace(caseId))
            throw new ArgumentException("caseId is required.");

        var basePath = Path.Combine(_env.ContentRootPath, "Workbooks");
        var caseFolder = Path.Combine(basePath, caseId);

        if (!Directory.Exists(caseFolder))
            return Task.FromResult<object>(Array.Empty<object>());

        var files = Directory.GetFiles(caseFolder)
            .Select(f => new
            {
                FileName = Path.GetFileName(f),
                CreatedOn = System.IO.File.GetCreationTime(f),
                ModifiedOn = System.IO.File.GetLastWriteTime(f),
                SizeBytes = new FileInfo(f).Length
            })
            .ToList();

        return Task.FromResult<object>(files);
    }
}
