namespace CaseManagement.SessionBillResolvers.V2;

public interface IBillingRepository
{
    Task SaveInvoiceAsync(Invoice invoice, CancellationToken ct);
}