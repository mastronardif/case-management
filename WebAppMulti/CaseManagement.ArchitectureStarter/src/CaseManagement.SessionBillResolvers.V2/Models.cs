namespace CaseManagement.SessionBillResolvers.V2;

public class BillingSettings
{
    public int BatchSize { get; set; } = 100;
    public string Mode { get; set; } = "Default";
}

public class SessionData
{
    public int SessionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
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