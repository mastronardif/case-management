namespace CaseManagement.SessionBillResolvers.V2;

public class BillingCalculator : IBillingCalculator
{
    public Invoice Calculate(SessionData session)
    {
        Console.WriteLine($"Calculating invoice for session {session.SessionId}, case {session.CaseNumber}");

        Console.WriteLine("""

                    //***

            SP returns billing JSON

            C# resolver deserializes

            Rules applied

            Invoice created

            Audit projection written

            ///

            SP returns billing JSON
            C# resolver deserializes
            Rules applied
            Invoice created
            Audit projection written
            """);

        return new Invoice
        {
            SessionId = session.SessionId,
            PatientName = session.PatientName,
            Amount = 0m  // TODO: implement real billing logic
        };
    }
}