//using System.Text.Json;
//using WebAppMulti.Models.Corqs;

//public static class SchemaLoader
//{
//    public static SchemaDefinition Load(string path)
//    {
//        var json = File.ReadAllText(path);

//        return JsonSerializer.Deserialize<SchemaDefinition>(json,
//            new JsonSerializerOptions
//            {
//                PropertyNameCaseInsensitive = true
//            }) ?? new SchemaDefinition();
//    }
//}
