namespace CaseManagement.SessionBillResolvers.V2;

public class BillingEngine
{
    private readonly ISessionProvider _sessions;
    private readonly IBillingCalculator _calculator;
    private readonly IBillingRepository _repository;

    public BillingEngine(
        ISessionProvider sessions,
        IBillingCalculator calculator,
        IBillingRepository repository)
    {
        _sessions = sessions;
        _calculator = calculator;
        _repository = repository;
    }

    public async Task<BillingResult> ProcessAsync(CancellationToken ct)
    {
        var sessions = await _sessions.GetUnbilledSessionsAsync(ct);

        var count = 0;

        foreach (var session in sessions)
        {
            var invoice = _calculator.Calculate(session);
            await _repository.SaveInvoiceAsync(invoice, ct);
            count++;
        }

        return new BillingResult { InvoiceCount = count };
    }
}