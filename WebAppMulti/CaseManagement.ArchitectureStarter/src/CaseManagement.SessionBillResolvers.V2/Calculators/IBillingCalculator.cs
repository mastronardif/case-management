namespace CaseManagement.SessionBillResolvers.V2;

public interface IBillingCalculator
{
    Invoice Calculate(SessionData session);
}