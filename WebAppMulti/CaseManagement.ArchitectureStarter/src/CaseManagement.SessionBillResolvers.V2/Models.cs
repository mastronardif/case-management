namespace CaseManagement.SessionBillResolvers.V2;

public enum BillingRunMode { Loop, SingleRun }

public record BillingRunOptions(BillingRunMode Mode, string? CaseNumber = null, int? SessionNumber = null);


public class BillingSettings
{
    public int BatchSize { get; set; } = 100;
    public string Mode { get; set; } = "Default";
}

public class SessionData
{
    public int SessionId { get; set; }
    public int CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class Invoice
{
    public int SessionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BillingResult
{
    public int InvoiceCount { get; set; }
}