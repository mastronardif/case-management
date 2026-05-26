using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class BillingProcessor
{
    private readonly ICaseManagementRepository _repository;
    private readonly IBillingCalculator _calculator;
    private readonly ILogger<BillingProcessor> _logger;

    public BillingProcessor(
        ICaseManagementRepository repository,
        IBillingCalculator calculator,
        ILogger<BillingProcessor> logger)
    {
        _repository = repository;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task RunAsync(BillingRunOptions options, CancellationToken ct)
    {
        if (options.Mode == BillingRunMode.SingleRun)
        {
            await ProcessOnceAsync(options, ct);
            return;
        }

        _logger.LogInformation("Billing processor running in loop mode");
        while (!ct.IsCancellationRequested)
        {
            await ProcessOnceAsync(options, ct);
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task ProcessOnceAsync(BillingRunOptions options, CancellationToken ct)
    {
        _logger.LogInformation("Billing cycle started");
        var sessions = await _repository.GetUnbilledSessionsAsync(options, ct);
        var count = 0;

        foreach (var session in sessions)
        {
            // TODO: replace stub DocumentIds with real context lookup per session/case
            var projectionDoc      = await _repository.GetDocumentAsync(new DocumentContext(DocumentId: 40), ct);
            var billingRuleDoc     = await _repository.GetDocumentAsync(new DocumentContext(DocumentId: 38), ct);
            var sessionExtractionDoc = await _repository.GetDocumentAsync(new DocumentContext(DocumentId: 39), ct);

            var invoiceJson = _calculator.Calculate(
                projectionDoc?.Content      ?? "",
                billingRuleDoc?.Content     ?? "",
                sessionExtractionDoc?.Content ?? "");

            var saveContext = new DocumentContext(
                DocumentId: sessionExtractionDoc?.DocumentId,
                CaseId:     session.CaseId,
                SessionId:  session.SessionId);

            // TODO: save projection for audit purposes
            await _repository.SaveInvoiceAsync(saveContext, invoiceJson, ct);
            count++;
        }

        _logger.LogInformation("Billing cycle complete. Invoices: {Count}", count);
    }
}