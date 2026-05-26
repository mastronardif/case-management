namespace CaseManagement.SessionBillResolvers.V2;

public interface IBillingCalculator
{
    string Calculate(string projectionDefinition, string billingRule, string sessionExtraction);
}