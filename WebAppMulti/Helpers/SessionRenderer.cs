using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;
namespace WebAppMulti.Helpers
{
    public static class SessionRenderer
    {
        // Simple token replacement: replaces {{Key}} with value.ToString()
        // jsonPath = path to JSON file (or you can pass the JSON string directly)
        // templatePath = path to HTML template file (or template string)
        public static string HtmlSession(string jsonPathOrContent, string templatePathOrContent, bool inputsAreFiles = true)
        {
            string jsonText = inputsAreFiles ? File.ReadAllText(jsonPathOrContent) : jsonPathOrContent;
            string template = inputsAreFiles ? File.ReadAllText(templatePathOrContent) : templatePathOrContent;

            // Parse JSON into dictionary
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText, opts)
                       ?? new Dictionary<string, object>();

            // Replace tokens like {{Key}} with values (HTML-escape values)
            foreach (var kv in dict)
            {
                var token = "{{" + kv.Key + "}}";
                var value = kv.Value?.ToString() ?? string.Empty;

                // HTML-escape to avoid injection
                var escaped = HtmlEncoder.Default.Encode(value);

                template = template.Replace(token, escaped);
            }

            return template;
        }

        public static string test_HtmlSession(string fnJson, string fnTemplate)
        {
            string html = SessionRenderer.HtmlSession("C:\\Users\\mastronardif\\Downloads\\session.json.json", "C:\\Users\\mastronardif\\Downloads\\session.template.html");
            return html;

        }
    }
}
