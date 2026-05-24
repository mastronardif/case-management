namespace CaseManagement.SessionBillResolvers.V2;

public class BillingCalculator : IBillingCalculator
{
    public Invoice Calculate(SessionData session) => new()
    {
        SessionId = session.SessionId,
        PatientName = session.PatientName,
        Amount = 0m  // TODO: implement real billing logic


        /****
          
        SP returns billing JSON
        
        C# resolver deserializes
        
        Rules applied
        
        Invoice created
        
        Audit projection written

        **/

    };
}