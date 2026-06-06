using System.Data;
using System.Text;
using System.Text.Json.Nodes;
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

    public Task<int> SaveDocumentAsync(DocumentContext context, string content, string documentType, string fileName, string contentType, CancellationToken ct)
        => SaveDocumentAsync(context, System.Text.Encoding.UTF8.GetBytes(content), documentType, fileName, contentType, ct);

    public async Task<int> SaveDocumentAsync(DocumentContext context, byte[] data, string documentType, string fileName, string contentType, CancellationToken ct)
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
        cmd.Parameters.AddWithValue("@DocumentType", documentType);
        cmd.Parameters.AddWithValue("@Title",        documentType);
        cmd.Parameters.AddWithValue("@FileName",     (object?)fileName            ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContentType",  contentType);
        cmd.Parameters.AddWithValue("@FileData",     data);
        cmd.Parameters.AddWithValue("@CreatedBy",    "SessionBillResolvers.V2");

        var docIdParam = new SqlParameter("@DocumentId", SqlDbType.Int)
        {
            Direction = ParameterDirection.InputOutput,
            Value     = (object?)context.DocumentId ?? DBNull.Value
        };
        cmd.Parameters.Add(docIdParam);

        await cmd.ExecuteNonQueryAsync(ct);

        return (int)docIdParam.Value;
    }

    public async Task<Claim837PData> Get837PDataAsync(int caseId, int sessionId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching 837P data. CaseId={CaseId}, SessionId={SessionId}", caseId, sessionId);

        await using var conn = new SqlConnection(_conn.DefaultConnection);
        await conn.OpenAsync(ct);

        using var cmd = new SqlCommand("[cases].[usp_Get837P]", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@CaseId",    caseId);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        using var r = await cmd.ExecuteReaderAsync(ct);

        var session       = await ReadSingleAsync(r, ct);   // RS1 Session
        await r.NextResultAsync(ct);
        var caseRow       = await ReadSingleAsync(r, ct);   // RS2 Case
        await r.NextResultAsync(ct);
        var coverage      = await ReadAllAsync(r, ct);      // RS3 InsuranceCoverage + Payer
        await r.NextResultAsync(ct);
        var authorization = await ReadAllAsync(r, ct);      // RS4 Authorization
        await r.NextResultAsync(ct);
        var diagnoses     = await ReadAllAsync(r, ct);      // RS5 Diagnosis
        await r.NextResultAsync(ct);
        var provider      = await ReadAllAsync(r, ct);      // RS6 Provider
        await r.NextResultAsync(ct);

        // RS7: definition doc — FileData bytes from cases.Document where DocumentId = 174
        JsonNode? definition = null;
        if (await r.ReadAsync(ct))
        {
            var fdIdx = r.GetOrdinal("FileData");
            if (!r.IsDBNull(fdIdx))
            {
                var bytes = (byte[])r.GetValue(fdIdx);
                try   { definition = JsonNode.Parse(Encoding.UTF8.GetString(bytes)); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse 837P definition FileData"); }
            }
        }

        return new Claim837PData(session, caseRow, coverage, authorization, diagnoses, provider, definition);
    }

    private static async Task<JsonObject?> ReadSingleAsync(SqlDataReader r, CancellationToken ct)
        => await r.ReadAsync(ct) ? RowToJson(r) : null;

    private static async Task<IReadOnlyList<JsonObject>> ReadAllAsync(SqlDataReader r, CancellationToken ct)
    {
        var list = new List<JsonObject>();
        while (await r.ReadAsync(ct))
            list.Add(RowToJson(r));
        return list;
    }

    private static JsonObject RowToJson(SqlDataReader r)
    {
        var obj     = new JsonObject();
        JsonNode? docNode = null;

        for (int i = 0; i < r.FieldCount; i++)
        {
            if (r.IsDBNull(i)) continue;
            var col = r.GetName(i);

            if (col.Equals("DocumentJson", StringComparison.OrdinalIgnoreCase))
            {
                try { docNode = JsonNode.Parse(r.GetString(i)); }
                catch { /* skip malformed */ }
                continue;
            }
            if (col.Equals("FileData", StringComparison.OrdinalIgnoreCase))
                continue;

            var node = ColumnToNode(r.GetValue(i));
            if (node is not null)
                obj[ToCamelCase(col)] = node;
        }

        if (docNode is not null)
            obj["doc"] = docNode;

        return obj;
    }

    private static JsonNode? ColumnToNode(object value) => value switch
    {
        int i       => JsonValue.Create(i),
        long l      => JsonValue.Create(l),
        short s     => JsonValue.Create((int)s),
        byte b      => JsonValue.Create((int)b),
        bool bl     => JsonValue.Create(bl),
        string s    => JsonValue.Create(s),
        decimal d   => JsonValue.Create(d),
        double d    => JsonValue.Create(d),
        float f     => JsonValue.Create((double)f),
        DateTime dt => JsonValue.Create(dt.ToString("O")),
        Guid g      => JsonValue.Create(g.ToString()),
        _           => JsonValue.Create(value.ToString())
    };

    private static string ToCamelCase(string s) =>
        s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..];

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