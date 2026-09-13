namespace CaseManagement.SessionBillResolvers.V2.Jobs;

// QueueClaimRow/ClaimInfo are never Dapper-materialized (constructed manually from scalar/output
// param results), so they're the only two left as plain positional records. Everything Dapper
// maps a multi-column row onto is property-based ({ get; set; }) instead — positional-record
// materialization proved unreliable here (Dapper 2.1.35 failed to match a constructor whenever a
// query omitted a trailing optional param, or had a DateTime? param, even with matching names).
public record QueueClaimRow(int QueueClaimId, int? SpecDocumentId);

public record ClaimInfo(int ClaimId, string ClaimNumber);

public record PracticeConfiguration
{
    public string? SubmitterName { get; set; }
    public string? SubmitterIdentifier { get; set; }
    public string? SenderIdQualifier { get; set; }
    public string? SenderId { get; set; }
    public string? ReceiverIdQualifier { get; set; }
    public string? ReceiverId { get; set; }
    public string? FunctionalIdentifierCode { get; set; }
    public string? VersionIdentifier { get; set; }
    public string? TestIndicator { get; set; }
    public string? BillingProviderName { get; set; }
    public string? BillingProviderNPI { get; set; }
    public string? BillingProviderTaxonomy { get; set; }
    public string? TaxId { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
}

public record InsuranceCoverage
{
    public int InsuranceCoverageId { get; set; }
    public int PayerId { get; set; }
    public string? PayerName { get; set; }
    public string? MemberId { get; set; }
    public string? GroupNumber { get; set; }
    public string? SubscriberName { get; set; }
    public string? RelationshipCode { get; set; }
    public string? SubscriberFirstName { get; set; }
    public string? SubscriberLastName { get; set; }
    public string? SubscriberMiddleName { get; set; }
    public DateTime? SubscriberDateOfBirth { get; set; }
    public string? SubscriberGender { get; set; }

    // Session projection/rule field names are camelCase and expect yyyyMMdd, not a raw DateTime.
    public string? SubscriberDateOfBirthFormatted => SubscriberDateOfBirth?.ToString("yyyyMMdd");
}

public record PayerEdi
{
    public string? PayerIdentifier { get; set; }
    public string? PayerIdentifierQualifier { get; set; }
}

public record PayerEdiIdentifiers
{
    public string? ReceiverId { get; set; }
    public string? SubmitterId { get; set; }
}

// GetPracticeBcbaAsync's query omits ProviderRole (all rows it returns are already known to be
// 'BCBA') — property-based avoids Dapper's positional-constructor matching entirely.
public record ProviderInfo
{
    public int ProviderId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? NPI { get; set; }
    public string? TaxonomyCode { get; set; }
    public string? ProviderRole { get; set; }
}

public record PatientInfo
{
    public int PatientId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    public string? DateOfBirthFormatted => DateOfBirth?.ToString("yyyyMMdd");
}

public record FeeScheduleLine
{
    public int MinutesPerUnit { get; set; }
    public decimal AllowedAmount { get; set; }
}
