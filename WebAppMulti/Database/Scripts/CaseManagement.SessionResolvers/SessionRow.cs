namespace CaseManagement.SessionResolvers.Resolvers;

public class SessionRow
{
    public int SessionId { get; set; }
    public int DocumentId { get; set; }
    public int PatientId { get; set; }
    public string? Type { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Notes { get; set; }
}