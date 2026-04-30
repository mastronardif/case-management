 
using WebAppMulti.Database.Repository;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

    private static async Task<IResult> SearchCases(DapperRepository db)
    {
        var result = await db.ExecuteStoredProcedureAsync(
            "cases.usp_SearchCases",
            new Dictionary<string, object?>
            {
            { "ClientId", null }
            });

        return Results.Ok(result);
    }





}
