using WebAppMulti.Database.Repository;

public class GetInvoiceDetailsHandler : ICorqsHandler
{
    private readonly DapperRepository _db;

    public string Name => "GetInvoiceDetails";

    public GetInvoiceDetailsHandler(DapperRepository db)
    {
        _db = db;
    }

    public async Task<object> ExecuteAsync(Dictionary<string, object?> input)
    {
        if (!input.TryGetValue("invoiceId", out var raw) || raw is null)
            throw new ArgumentException("invoiceId is required.");

        var sets = await _db.RunNamedMultiResultAsync(
            "cases.usp_Invoice_GetDetails",
            new Dictionary<string, object?> { ["invoiceId"] = raw },
            ["invoice", "lines", "audit"]);

        return new
        {
            invoice = sets.GetValueOrDefault("invoice")?.FirstOrDefault(),
            lines   = sets.GetValueOrDefault("lines")   ?? [],
            audit   = sets.GetValueOrDefault("audit")   ?? [],
        };
    }
}
