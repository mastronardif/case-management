using MediatR;

public class SearchCasesHandler : IRequestHandler<SearchCasesQuery, List<object>>
{
    public Task<List<object>> Handle(SearchCasesQuery request, CancellationToken ct)
    {
        var cases = new List<object>
        {
            new { userId = 1, id = 1, title = "quidem molestiae enim" },
            new { userId = 1, id = 2, title = "sunt qui excepturi placeat culpa" },
            new { userId = 1, id = 3, title = "omnis laborum odio" }
        };

        return Task.FromResult(cases);
    }
}
