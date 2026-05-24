using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class BillingProcessor
{
    private readonly ISessionProvider _sessions;
    private readonly IBillingCalculator _calculator;
    private readonly IBillingRepository _repository;
    private readonly ILogger<BillingProcessor> _logger;

    public BillingProcessor(
        ISessionProvider sessions,
        IBillingCalculator calculator,
        IBillingRepository repository,
        ILogger<BillingProcessor> logger)
    {
        _sessions = sessions;
        _calculator = calculator;
        _repository = repository;
        _logger = logger;
    }

    public async Task RunAsync(BillingRunOptions options, CancellationToken ct)
    {
        if (options.Mode == BillingRunMode.SingleRun)
        {
            await ProcessOnceAsync(ct);
            return;
        }

        _logger.LogInformation("Billing processor running in loop mode");
        while (!ct.IsCancellationRequested)
        {
            await ProcessOnceAsync(ct);
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken ct)
    {
        _logger.LogInformation("Billing cycle started");
        var sessions = await _sessions.GetUnbilledSessionsAsync(ct);
        var count = 0;

        foreach (var session in sessions)
        {
            var invoice = _calculator.Calculate(session);
            await _repository.SaveInvoiceAsync(invoice, ct);
            count++;
        }

        _logger.LogInformation("Billing cycle complete. Invoices: {Count}", count);
    }
}