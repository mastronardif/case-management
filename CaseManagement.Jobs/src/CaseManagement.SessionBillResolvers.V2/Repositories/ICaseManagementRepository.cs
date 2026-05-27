namespace CaseManagement.SessionBillResolvers.V2;

public interface ICaseManagementRepository
{
    Task<IEnumerable<SessionData>> GetUnbilledSessionsAsync(BillingRunOptions options, CancellationToken ct);
    Task<Document?> GetDocumentAsync(DocumentContext context, CancellationToken ct);
    Task SaveInvoiceAsync(DocumentContext context, string invoiceJson, CancellationToken ct);
    Task<int> SaveDocumentAsync(DocumentContext context, string content, string documentType, string fileName, CancellationToken ct);
}