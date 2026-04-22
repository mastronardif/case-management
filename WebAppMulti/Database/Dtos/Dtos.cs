namespace WebAppMulti.Database.Dtos.CaseManagement
{


    // ============================
    // CREATE SESSION
    // ============================
    public class SessionCreateDto
    {
        public int CaseId { get; set; }
        public DateTime SessionDate { get; set; }
        public string? Notes { get; set; }
    }

    // ============================
    // RETURN SESSION
    // ============================
    public class SessionDto
    {
        public int SessionId { get; set; }
        public int CaseId { get; set; }
        public DateTime SessionDate { get; set; }
        public string? Notes { get; set; }
        public List<DocumentDto> Documents { get; set; } = new();
    }

    // ============================
    // DOCUMENT INSIDE SESSION DTO
    // ============================
    public class DocumentDto
    {
        public int DocumentId { get; set; }
        public int FormTemplateId { get; set; }
        public Dictionary<string, object> FormData { get; set; } = new();
    }
}
