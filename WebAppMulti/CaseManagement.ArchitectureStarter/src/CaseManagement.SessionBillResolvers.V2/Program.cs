using CaseManagement.Shared;

Console.WriteLine("CaseManagement.SessionBillResolvers.V2");

var session = new SessionContext
{
    SessionId = 1001,
    PatientName = "Test Patient"
};

Console.WriteLine($"Session: {session.SessionId}");
Console.WriteLine($"Patient: {session.PatientName}");
Console.WriteLine("Hello World - Billing Resolver Pipeline Started");
