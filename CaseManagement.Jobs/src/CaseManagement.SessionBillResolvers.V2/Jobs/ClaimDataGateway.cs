using System.Data;
using CaseManagement.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CaseManagement.SessionBillResolvers.V2.Jobs;

// Direct SQL access for the clearinghouse queue job — the in-process replacement for
// BuildQueueForClearingHouse.ps1's ~15 raw System.Data.SqlClient Get-X/Set-X/Add-X functions.
// Uses Microsoft.Data.SqlClient (the modern, genuinely cross-platform driver), same as the
// rest of this project — the PS1 script's System.Data.SqlClient was the one part of that
// script that wouldn't have run reliably on Linux even under pwsh.
public class ClaimDataGateway(ConnectionSettings conn, ILogger<ClaimDataGateway> logger)
{
    private SqlConnection Open() => new(conn.DefaultConnection);

    public async Task<QueueClaimRow?> GetQueueClaimAsync(int queueClaimId, CancellationToken ct)
    {
        await using var db = Open();
        var specDocId = await db.QuerySingleOrDefaultAsync<int?>(
            "SELECT SpecDocumentId FROM cases.queueClaimsToBeCreated WHERE QueueClaimId = @queueClaimId",
            new { queueClaimId });
        return specDocId is null ? null : new QueueClaimRow(queueClaimId, specDocId);
    }

    public async Task SetQueueClaimStatusAsync(int queueClaimId, string status, CancellationToken ct)
    {
        await using var db = Open();
        await db.ExecuteAsync(
            "UPDATE cases.queueClaimsToBeCreated SET Status = @status WHERE QueueClaimId = @queueClaimId",
            new { status, queueClaimId });
    }

    public async Task<int?> GetActiveRuleDocIdAsync(string ruleName, CancellationToken ct)
    {
        await using var db = Open();
        var result = await db.QuerySingleOrDefaultAsync<int?>(
            """
            SELECT TOP 1 RuleDocumentId FROM [cases].[ProjectorRule]
            WHERE Name = @ruleName AND IsActive = 1
            ORDER BY ProjectorRuleId DESC
            """, new { ruleName });
        return result is null or 0 ? null : result;
    }

    public async Task<int?> GetActiveProjectionDocIdAsync(string projectionName, CancellationToken ct)
    {
        await using var db = Open();
        var result = await db.QuerySingleOrDefaultAsync<int?>(
            """
            SELECT TOP 1 ProjectionDocumentId FROM [cases].[ProjectorRule]
            WHERE Name = @projectionName AND IsActive = 1
            ORDER BY ProjectorRuleId DESC
            """, new { projectionName });
        return result is null or 0 ? null : result;
    }

    public async Task<ClaimInfo> CreateClaimAsync(int caseId, IEnumerable<int> sessionDocumentIds, CancellationToken ct)
    {
        var sessionDocumentIdsCsv = string.Join(",", sessionDocumentIds);

        await using var db = Open();
        var parameters = new DynamicParameters();
        parameters.Add("@CaseId", caseId);
        parameters.Add("@SessionDocumentIds", sessionDocumentIdsCsv);
        parameters.Add("@ClaimId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("@ClaimNumber", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

        await db.ExecuteAsync("[cases].[usp_CreateClaim]", parameters, commandType: CommandType.StoredProcedure);

        return new ClaimInfo(parameters.Get<int>("@ClaimId"), parameters.Get<string>("@ClaimNumber"));
    }

    public async Task SetClaimEdiDocumentAsync(
        int claimId, int ediDocumentId, string status, int sourcesDocumentId, int availityReviewDocumentId, CancellationToken ct)
    {
        await using var db = Open();
        await db.ExecuteAsync(
            """
            UPDATE [cases].[Claim]
            SET    EdiDocumentId = @ediDocumentId, Status = @status, SourcesDocumentId = @sourcesDocumentId,
                   AvailityReviewDocumentId = @availityReviewDocumentId
            WHERE  ClaimId = @claimId
            """, new { ediDocumentId, status, sourcesDocumentId, availityReviewDocumentId, claimId });
    }

    public async Task AddClaimToSubmitQueueAsync(int claimId, int ediDocumentId, CancellationToken ct)
    {
        await using var db = Open();
        await db.ExecuteAsync(
            """
            INSERT INTO [cases].[queueClaimsToBeSubmitted] (ClaimId, EdiDocumentId, Status)
            VALUES (@claimId, @ediDocumentId, 'Pending')
            """, new { claimId, ediDocumentId });
    }

    // ClaimId/QueueClaimId/DocumentId are all optional so an event can anchor to whichever ids
    // are known at that point — mirrors Add-ClaimPipelineEvent's nullable params exactly.
    public async Task AddClaimPipelineEventAsync(
        int caseId, int? claimId, int? queueClaimId, string eventType, int? documentId, string? details, CancellationToken ct)
    {
        await using var db = Open();
        await db.ExecuteAsync(
            """
            INSERT INTO [cases].[ClaimPipelineEvent] (CaseId, ClaimId, QueueClaimId, EventType, DocumentId, Details)
            VALUES (@caseId, @claimId, @queueClaimId, @eventType, @documentId, @details)
            """, new { caseId, claimId, queueClaimId, eventType, documentId, details });
    }

    public async Task<PracticeConfiguration> GetPracticeConfigurationAsync(CancellationToken ct)
    {
        await using var db = Open();
        var config = await db.QuerySingleOrDefaultAsync<PracticeConfiguration>(
            """
            SELECT TOP 1
                SubmitterName, SubmitterIdentifier,
                SenderIdQualifier, SenderId,
                FunctionalIdentifierCode, VersionIdentifier, TestIndicator,
                BillingProviderName, BillingProviderNPI, BillingProviderTaxonomy, TaxId,
                Address1, Address2, City, State, Zip
            FROM   [cases].[PracticeConfiguration]
            WHERE  IsActive = 1
            ORDER  BY PracticeConfigurationId DESC
            """);

        return config ?? throw new InvalidOperationException("No active row found in [cases].[PracticeConfiguration].");
    }

    public async Task<InsuranceCoverage?> GetInsuranceCoverageAsync(int caseId, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<InsuranceCoverage>(
            """
            SELECT TOP 1
                IC.InsuranceCoverageId, IC.PayerId, IC.MemberId, IC.GroupNumber, IC.SubscriberName,
                IC.RelationshipCode, IC.SubscriberFirstName, IC.SubscriberLastName, IC.SubscriberMiddleName,
                IC.SubscriberDateOfBirth, IC.SubscriberGender,
                PP.PayerName
            FROM   [cases].[InsuranceCoverage] IC
            LEFT JOIN [cases].[Payer] PP ON PP.PayerId = IC.PayerId
            WHERE  IC.CaseId = @caseId
            ORDER  BY IC.InsuranceCoverageId DESC
            """, new { caseId });
    }

    // Payer.PayerCode/CodeQualifier are legacy — PayerEDI is the authoritative source for the
    // payer's own EDI identifier (2010BB.NM1.NM108/NM109).
    public async Task<PayerEdi?> GetPayerEdiAsync(int payerId, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<PayerEdi>(
            """
            SELECT TOP 1 PayerIdentifier, PayerIdentifierQualifier
            FROM   [cases].[PayerEDI]
            WHERE  PayerId = @payerId AND IsActive = 1
            ORDER  BY PayerEDIId DESC
            """, new { payerId });
    }

    // Availity trading-partner identity for this payer (ISA08/GS03 receiver, and — when a payer
    // requires a different submitter enrollment than the practice-wide default — ISA06/GS02
    // sender). Both columns are blank/dummy for most payers right now.
    public async Task<PayerEdiIdentifiers?> GetPayerEdiIdentifiersAsync(int payerId, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<PayerEdiIdentifiers>(
            "SELECT EDIReceiverId AS ReceiverId, AvailitySubmitterId AS SubmitterId FROM [cases].[Payer] WHERE PayerId = @payerId",
            new { payerId });
    }

    // The billable/credentialed rendering provider for an RBT-run session is the supervising
    // BCBA, not the RBT — single-practice-wide assumption for now (one active
    // ProviderRole='BCBA' row) — revisit if a second BCBA is added.
    public async Task<ProviderInfo> GetPracticeBcbaAsync(CancellationToken ct)
    {
        await using var db = Open();
        var rows = (await db.QueryAsync<ProviderInfo>(
            """
            SELECT Id AS ProviderId, FirstName, LastName, NPI, TaxonomyCode
            FROM   [cases].[Provider]
            WHERE  ProviderRole = 'BCBA' AND IsActive = 1
            """)).ToList();

        if (rows.Count == 0)
            throw new InvalidOperationException("No active ProviderRole='BCBA' row found in cases.Provider — can't resolve a rendering provider for RBT-run sessions.");
        if (rows.Count > 1)
            throw new InvalidOperationException("Multiple active ProviderRole='BCBA' rows found — GetPracticeBcbaAsync assumes exactly one.");

        return rows[0];
    }

    public async Task<ProviderInfo?> GetProviderByClinicianUsernameAsync(string clinicianUsername, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<ProviderInfo>(
            """
            SELECT TOP 1 Id AS ProviderId, FirstName, LastName, NPI, TaxonomyCode, ProviderRole
            FROM   [cases].[Provider]
            WHERE  ClinicianUsername = @clinicianUsername AND IsActive = 1
            """, new { clinicianUsername });
    }

    // Feeds 2010CA (Patient) only — 2010BA (Subscriber) comes from InsuranceCoverage, never
    // from here, even when patient and subscriber are the same person.
    public async Task<PatientInfo?> GetPatientAsync(int caseId, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<PatientInfo>(
            """
            SELECT TOP 1 Id AS PatientId, FirstName, LastName, DateOfBirth, Gender
            FROM   [cases].[Patient]
            WHERE  CaseId = @caseId AND IsActive = 1
            ORDER  BY Id DESC
            """, new { caseId });
    }

    // Drives billing.minutesPerUnit/allowedAmount ("how much to bill"). Resolved per session
    // (not once per claim) since different sessions can bill different procedure codes.
    public async Task<FeeScheduleLine?> GetFeeScheduleLineAsync(int payerId, string procedureCode, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<FeeScheduleLine>(
            """
            SELECT TOP 1 fsl.MinutesPerUnit, fsl.AllowedAmount
            FROM   [cases].[FeeSchedule] fs
            JOIN   [cases].[FeeScheduleLine] fsl ON fsl.FeeScheduleId = fs.FeeScheduleId
            WHERE  fs.PayerId = @payerId AND fsl.ProcedureCode = @procedureCode
              AND  fs.IsActive = 1 AND fsl.IsActive = 1
            ORDER  BY fsl.FeeScheduleLineId DESC
            """, new { payerId, procedureCode });
    }

    // The original uploaded PDF/scan each session was extracted from — kept in the
    // claim-sources paper trail alongside JsonDocumentId.
    public async Task<int?> GetSessionSourceDocIdAsync(int jsonDocumentId, CancellationToken ct)
    {
        await using var db = Open();
        return await db.QuerySingleOrDefaultAsync<int?>(
            "SELECT TOP 1 SourceDocumentId FROM [cases].[Session] WHERE JsonDocumentId = @jsonDocumentId",
            new { jsonDocumentId });
    }
}
