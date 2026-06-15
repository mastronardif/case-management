namespace CaseManagement.SessionBillResolvers.V2;

public interface ICaseManagementRepository
{
    Task<IEnumerable<SessionData>> GetUnbilledSessionsAsync(BillingRunOptions options, CancellationToken ct);
    Task<Document?> GetDocumentAsync(DocumentContext context, CancellationToken ct);
    Task SaveInvoiceAsync(DocumentContext context, string invoiceJson, CancellationToken ct);
    Task<int> SaveDocumentAsync(DocumentContext context, string content, string documentType, string fileName, string contentType, CancellationToken ct);
    Task<int> SaveDocumentAsync(DocumentContext context, byte[] data, string documentType, string fileName, string contentType, CancellationToken ct);
    Task<Claim837PData> Get837PDataAsync(int caseId, int sessionId, CancellationToken ct);
    Task ResolveDocAsync(int docId, string tableName, int caseId, int? srcDocId, CancellationToken ct);
}