//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Configuration;
//using System.Data;
//using System.Text.Json;

//namespace CaseManagement.SessionResolvers.Resolvers;

//public class SessionImportResolver
//{
//    private readonly IConfiguration _config;

//    public SessionImportResolver(IConfiguration config)
//    {
//        _config = config;
//    }

//    public async Task RunAsync(string jsonFile)
//    {
//        var connectionString =
//            _config.GetConnectionString("DefaultConnection");

//        var json = await File.ReadAllTextAsync(jsonFile);

//        var rows = JsonSerializer.Deserialize<List<SessionRow>>(json);

//        if (rows == null || rows.Count == 0)
//            return;

//        using var conn = new SqlConnection(connectionString);
//        await conn.OpenAsync();

//        foreach (var row in rows)
//        {
//            using var cmd = new SqlCommand("[cases].[usp_Session_Upsert]", conn);

//            cmd.CommandType = CommandType.StoredProcedure;

//            cmd.Parameters.Add("@SessionId", SqlDbType.Int)
//                .Value = row.SessionId;

//            cmd.Parameters.Add("@DocumentId", SqlDbType.Int)
//                .Value = row.DocumentId;

//            cmd.Parameters.Add("@PatientId", SqlDbType.Int)
//                .Value = row.PatientId;

//            cmd.Parameters.Add("@Type", SqlDbType.NVarChar, 50)
//                .Value = DbValue(row.Type);

//            cmd.Parameters.Add("@StartTime", SqlDbType.DateTime)
//                .Value = (object?)row.StartTime ?? DBNull.Value;

//            cmd.Parameters.Add("@EndTime", SqlDbType.DateTime)
//                .Value = (object?)row.EndTime ?? DBNull.Value;

//            cmd.Parameters.Add("@Notes", SqlDbType.NVarChar, -1)
//                .Value = DbValue(row.Notes);

//            await cmd.ExecuteNonQueryAsync();
//        }
//    }

//    private object DbValue(string? value)
//    {
//        return string.IsNullOrWhiteSpace(value)
//            ? DBNull.Value
//            : value.Trim();
//    }
//}