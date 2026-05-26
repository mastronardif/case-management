using CaseManagement.SessionBillResolvers.V2.Engine;
using System.Text.Json;

namespace CaseManagement.SessionBillResolvers.V2;

public class BillingCalculator : IBillingCalculator
{
    public string Calculate(string projectionDefinition, string billingRule, string sessionExtraction)
    {
        Console.WriteLine("""
            --- Billing Pipeline ---
            Clinical Document      (sessionExtraction)
                 ↓
            Projection Engine      (projectionDefinition)
                 ↓
            Billing Projection
                 ↓
            Billing Rules          (billingRule)
                 ↓
            Invoice
            -----------------------
            """);

        var projection = ProjectionTransformer.Transform(projectionDefinition, sessionExtraction);

        Console.WriteLine("\nProjection result:\n");
        Console.WriteLine(projection.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("\nProjection result: END\n");

        // TODO: apply billingRule to projection → produce Invoice
        return projection.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
