namespace WebAppMulti.Database.Models.CaseManagement
{
    public class FormTemplate
    {
        public int FormTemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string JsonSchema { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
