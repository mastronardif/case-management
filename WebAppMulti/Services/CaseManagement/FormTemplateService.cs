using System;
using WebAppMulti.Database;
using WebAppMulti.Database.Models.CaseManagement;
using WebAppMulti.Database.Repository;

namespace WebAppMulti.Services.CaseManagement
{
    public class FormTemplateService
    {
        private readonly MyDbContext _context;

        private readonly GenericRepository _repo;

        public FormTemplateService(GenericRepository repo)
        {
            _repo = repo;
        }


        /// Get HTML template by ID, optionally merge with JSON data
        /// </summary>
        public async Task<string> GetHtmlFromTemplate(int templateId, Dictionary<string, object>? jsonData = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@TemplateId", templateId }
            };

            var dt = await _repo.ExecuteStoredProcedureAsync("sp_GetFormTemplateById", parameters);

            if (dt.Rows.Count == 0)
                return string.Empty;

            var html = dt.Rows[0]["HtmlTemplate"]?.ToString() ?? string.Empty;

            if (jsonData != null && jsonData.Count > 0)
            {
                // Replace placeholders in the form {{Key}} with values from jsonData
                foreach (var kvp in jsonData)
                {
                    html = html.Replace("{{" + kvp.Key + "}}", kvp.Value?.ToString() ?? "");
                }
            }

            return html;
        }

        public async Task<FormTemplate> GetTemplate(int id)
        {
            // TEMP: No implementation yet.
            // Returning an empty FormTemplate so the app compiles and runs.
            return await Task.FromResult(new FormTemplate
            {
                FormTemplateId = id,
                Name = string.Empty,
                Description = string.Empty,
                JsonSchema = "{}",
                Version = 1,
                CreatedAt = DateTime.UtcNow
            });
  
        }
    }

}
