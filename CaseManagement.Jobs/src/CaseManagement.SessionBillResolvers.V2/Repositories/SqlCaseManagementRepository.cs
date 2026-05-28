using System.Data;
using CaseManagement.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2;

public class SqlCaseManagementRepository : ICaseManagementRepository
{
    private readonly ConnectionSettings _conn;
    private readonly ILogger<SqlCaseManagementRepository> _logger;

    public SqlCaseManagementRepository(ConnectionSettings conn, ILogger<SqlCaseManagementRepository> logger)
    {
        _conn = conn;
        _logger = logger;
    }

    public async Task<IEnumerable<SessionData>> GetUnbilledSessionsAsync(BillingRunOptions options, CancellationToken ct)
    {
        _logger.LogInformation("Fetching unbilled sessions. CaseNumber: {CaseNumber}, SessionNumber: {SessionNumber}",
            options.CaseNumber ?? "all", options.SessionNumber?.ToString() ?? "all");

        // TODO: remove mock when usp_GetUnbilledSessions is ready
        var mockSessions = new List<SessionData>
        {
            new SessionData { SessionId = 1, CaseNumber = "CASE123", DurationMinutes = 60 },
            new SessionData { SessionId = 2, CaseNumber = "CASE456", DurationMinutes = 30 }
        };
        if (options.Mode == BillingRunMode.SingleRun) mockSessions.RemoveAt(1);
        return mockSessions;

        var parameters = new DynamicParameters();
        parameters.Add("@CaseNumber", options.CaseNumber);
        parameters.Add("@SessionNumber", options.SessionNumber);

        await using var conn = new SqlConnection(_conn.DefaultConnection);
        return await conn.QueryAsync<SessionData>("usp_GetUnbilledSessions", parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task<Document?> GetDocumentAsync(DocumentContext context, CancellationToken ct)
    {
        _logger.LogInformation("Fetching document. DocumentId: {DocumentId}, SessionId: {SessionId}, CaseId: {CaseId}, DocumentType: {DocumentType}",
            context.DocumentId, context.SessionId, context.CaseId, context.DocumentType);

        await using var conn = new SqlConnection(_conn.DefaultConnection);
        await conn.OpenAsync(ct);

        using var cmd = new SqlCommand("[cases].[usp_Document_GetByContext]", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@DocumentId",   (object?)context.DocumentId  ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CaseId",       (object?)context.CaseId      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WorkbookQId",  (object?)context.WorkbookQId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SessionId",    (object?)context.SessionId   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DocumentType", (object?)context.DocumentType ?? DBNull.Value);

        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);

        if (!await reader.ReadAsync(ct))
            return null;

        // columns must be read in SELECT order due to SequentialAccess
        return new Document
        {
            DocumentId   = reader.GetInt32(reader.GetOrdinal("DocumentId")),
            VersionId    = reader.IsDBNull(reader.GetOrdinal("VersionId"))    ? null : reader.GetGuid(reader.GetOrdinal("VersionId")),
            CaseId       = reader.IsDBNull(reader.GetOrdinal("CaseId"))       ? null : reader.GetInt32(reader.GetOrdinal("CaseId")),
            WorkbookQId  = reader.IsDBNull(reader.GetOrdinal("WorkbookQId"))  ? null : reader.GetInt32(reader.GetOrdinal("WorkbookQId")),
            SessionId    = reader.IsDBNull(reader.GetOrdinal("SessionId"))    ? null : reader.GetInt32(reader.GetOrdinal("SessionId")),
            DocumentType = reader.IsDBNull(reader.GetOrdinal("DocumentType")) ? null : reader.GetString(reader.GetOrdinal("DocumentType")),
            Title        = reader.IsDBNull(reader.GetOrdinal("Title"))        ? null : reader.GetString(reader.GetOrdinal("Title")),
            FileName     = reader.IsDBNull(reader.GetOrdinal("FileName"))     ? null : reader.GetString(reader.GetOrdinal("FileName")),
            ContentType  = reader.IsDBNull(reader.GetOrdinal("ContentType"))  ? null : reader.GetString(reader.GetOrdinal("ContentType")),
            FileData     = (byte[])reader.GetValue(reader.GetOrdinal("FileData")),
            CreatedDate  = reader.IsDBNull(reader.GetOrdinal("CreatedDate"))  ? null : reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            CreatedBy    = reader.IsDBNull(reader.GetOrdinal("CreatedBy"))    ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
            IsActive     = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        };
    }

    public async Task<int> SaveDocumentAsync(DocumentContext context, string content, string documentType, string fileName, string contentType, CancellationToken ct)
    {
        _logger.LogInformation("Saving document. Type: {DocumentType}, CaseId: {CaseId}, SessionId: {SessionId}",
            documentType, context.CaseId, context.SessionId);

        await using var conn = new SqlConnection(_conn.DefaultConnection);
        await conn.OpenAsync(ct);

        using var cmd = new SqlCommand("[cases].[usp_Document_Save]", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CaseId",       (object?)context.CaseId      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SessionId",    (object?)context.SessionId   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@WorkbookQId",  (object?)context.WorkbookQId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CaseNumber",   DBNull.Value);
        cmd.Parameters.AddWithValue("@DocumentType", "Other");
        cmd.Parameters.AddWithValue("@Title",        documentType);
        cmd.Parameters.AddWithValue("@FileName",     (object?)fileName            ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContentType",  contentType);
        cmd.Parameters.AddWithValue("@FileData",     System.Text.Encoding.UTF8.GetBytes(content));
        cmd.Parameters.AddWithValue("@CreatedBy",    "SessionBillResolvers.V2");

        var docIdParam = new SqlParameter("@DocumentId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(docIdParam);

        await cmd.ExecuteNonQueryAsync(ct);

        return (int)docIdParam.Value;
    }

    public async Task SaveInvoiceAsync(DocumentContext context, string invoiceJson, CancellationToken ct)
    {
        _logger.LogInformation("Saving invoice");
        Console.WriteLine($"Saving invoice: {invoiceJson}");

        var fileName = $@"C:\temp\{context.DocumentId}.{context.CaseId}.{context.SessionId}.json";
        await File.WriteAllTextAsync(fileName, invoiceJson, ct);
        _logger.LogInformation("Invoice written to {FileName}", fileName);

        // TODO: uncomment when usp_SaveBillingInvoice is ready
        // await using var conn = new SqlConnection(_conn.DefaultConnection);
        // await conn.ExecuteAsync("usp_SaveBillingInvoice", new { InvoiceJson = invoiceJson }, commandType: CommandType.StoredProcedure);
    }
}