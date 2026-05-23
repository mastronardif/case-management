using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class BillingRunner
{
    private readonly BillingEngine _engine;
    private readonly ILogger<BillingRunner> _logger;

    public BillingRunner(BillingEngine engine, ILogger<BillingRunner> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Billing run started");

        var result = await _engine.ProcessAsync(ct);

        _logger.LogInformation("Billing run complete. Invoices: {Count}", result.InvoiceCount);
    }
}