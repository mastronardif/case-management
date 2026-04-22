namespace WebAppMulti.Database.Dtos.CaseManagement
{
    public class DocumentCreateDto
    {
        /// <summary>
        /// The ID of the form template (e.g., SOAP Note, Progress Note, etc.)
        /// </summary>
        public int FormTemplateId { get; set; }

        /// <summary>
        /// Key/value pairs of the form filled out by the user.
        /// </summary>
        public Dictionary<string, object> FormData { get; set; } = new();
    }

    public class SessionDocumentDto
    {
        public int DocumentId { get; set; }
        public int SessionId { get; set; }
        public int FormTemplateId { get; set; }
        public Dictionary<string, object> FormData { get; set; } = new();
    }
}
