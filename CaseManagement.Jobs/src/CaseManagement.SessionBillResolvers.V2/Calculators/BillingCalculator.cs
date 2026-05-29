using CaseManagement.SessionBillResolvers.V2.Engine;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        var projectionJson = projection.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        var evaluation     = BillingRuleEvaluator.Evaluate(projectionJson, billingRule);

        Console.WriteLine("\nInvoice result:\n");
        var invoiceJson = evaluation.Invoice.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(invoiceJson);
        if (evaluation.ValidationIssues.Count > 0)
        {
            Console.WriteLine("\nValidation issues:");
            foreach (var issue in evaluation.ValidationIssues)
                Console.WriteLine($"  - {issue}");
        }
        Console.WriteLine("\nInvoice result: END\n");

        return invoiceJson;
    }
}
