using Newtonsoft.Json;
using WebAppMulti.Models.Corqs;

namespace WebAppMulti.Models.Corqs
{
    public class SchemaRoot
    {
        [JsonProperty("apis")]
        public List<ApiDefinition> Apis { get; set; }
    }
}
