using System.Collections.Generic;

namespace WebAppMulti.Models.Corqs;

public class SchemaDefinition
{
    public List<ApiDefinition> Apis { get; set; } = new();
}
