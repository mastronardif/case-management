using MediatR;

public static class CasesEndpoints
{
    public static IEndpointRouteBuilder MapCasesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cases")
            .WithTags("Cases")
            .WithGroupName("Case Management");

        group.MapGet("/", SearchCases)
            .WithName("SearchCases")
            .WithSummary("Search cases")
            .WithDescription("Returns a list of cases.");

        return app;
    }

    private static async Task<IResult> SearchCases(ISender sender)
    {
        var result = await sender.Send(new SearchCasesQuery());
        return Results.Ok(result);
    }
}
