namespace CaseManagement.SessionBillResolvers.V2;

public class BillingCalculator : IBillingCalculator
{
    public Invoice Calculate(SessionData session) => new()
    {
        SessionId = session.SessionId,
        PatientName = session.PatientName,
        Amount = 0m  // TODO: implement real billing logic
    };
}