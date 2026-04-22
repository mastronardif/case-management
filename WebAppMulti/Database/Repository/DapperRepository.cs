using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace WebAppMulti.Database.Repository
{
    public class DapperRepository
    {
        private readonly string _connectionString;

        public DapperRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Convert Dictionary<string, object> to Dapper parameters
        private static DynamicParameters ToDynamicParameters(Dictionary<string, object>? parameters)
        {
            var dp = new DynamicParameters();
            if (parameters == null) return dp;

            foreach (var kvp in parameters)
            {
                dp.Add(kvp.Key, NormalizeJsonValue(kvp.Value));
            }

            return dp;
        }

        // 🔹 Single Stored Procedure (Sync)
        public IEnumerable<dynamic> ExecuteStoredProcedure(string spName, Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            return conn.Query(spName, ToDynamicParameters(parameters), commandType: CommandType.StoredProcedure);
        }

        // 🔹 Single Stored Procedure (Async)
        public async Task<IEnumerable<dynamic>> ExecuteStoredProcedureAsync(string spName, Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            return await conn.QueryAsync(spName, ToDynamicParameters(parameters), commandType: CommandType.StoredProcedure);
        }

        // 🔹 Multiple Stored Procedures (Sequential)
        public async Task<List<object>> ExecuteMultipleStoredProceduresAsync(
            List<(string SpName, Dictionary<string, object>? Parameters)> procedures)
        {
            var results = new List<object>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var proc in procedures)
            {
                var dp = ToDynamicParameters(proc.Parameters);
                var rows = await conn.QueryAsync(proc.SpName, dp, commandType: CommandType.StoredProcedure);
                results.Add(new { Name = proc.SpName, Rows = rows });
            }

            return results;
        }

        // 🔹 Multiple Stored Procedures (Parallel)
        public async Task<List<object>> ExecuteMultipleStoredProceduresParallelAsync(
            List<(string SpName, Dictionary<string, object>? Parameters)> procedures)
        {
            var tasks = procedures.Select(async proc =>
            {
                using var conn = new SqlConnection(_connectionString);
                var dp = ToDynamicParameters(proc.Parameters);
                var rows = await conn.QueryAsync(proc.SpName, dp, commandType: CommandType.StoredProcedure);
                return new { Name = proc.SpName, Rows = rows };
            });

            var resultsArray = await Task.WhenAll(tasks);
            return resultsArray.Cast<object>().ToList();
        }

        // 🔹 Normalize values like in your GenericRepository
        private static object NormalizeJsonValue(object? value)
        {
            if (value == null)
                return DBNull.Value;

            if (value is JsonElement json)
            {
                switch (json.ValueKind)
                {
                    case JsonValueKind.String:
                        return (object?)json.GetString() ?? DBNull.Value;
                    case JsonValueKind.Number:
                        if (json.TryGetInt32(out int i)) return i;
                        if (json.TryGetInt64(out long l)) return l;
                        if (json.TryGetDecimal(out decimal d)) return d;
                        if (json.TryGetDouble(out double dbl)) return dbl;
                        return json.GetRawText();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return json.GetBoolean();
                    case JsonValueKind.Null:
                        return DBNull.Value;
                    case JsonValueKind.Object:
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json.GetRawText());
                        return dict == null ? DBNull.Value : string.Join(",", dict.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                    case JsonValueKind.Array:
                        var list = json.EnumerateArray().Select(e => NormalizeJsonValue(e)).ToList();
                        return list.Count == 0 ? DBNull.Value : string.Join(",", list);
                    default:
                        return json.GetRawText();
                }
            }

            return value;
        }

        public async Task<object> RunAsync(string spName, Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);

            var dp = ToDynamicParameters(parameters);
            var rows = await conn.QueryAsync(spName, dp, commandType: CommandType.StoredProcedure);

            return new
            {
                StoredProcedure = spName,
                Rows = rows.ToList()
            };
        }

        public async Task<object> RunMultiResultAsync(string spName, Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);

            var dp = ToDynamicParameters(parameters);
            using var multi = await conn.QueryMultipleAsync(spName, dp, commandType: CommandType.StoredProcedure);

            var resultSets = new List<object>();

            while (!multi.IsConsumed)
                resultSets.Add(multi.Read().ToList());

            return new
            {
                StoredProcedure = spName,
                Results = resultSets
            };
        }

        public async Task<object> RunProceduresAsync(List<(string SpName, Dictionary<string, object>? Parameters)> procedures, bool parallel = false)
        {
            if (parallel)
            {
                var tasks = procedures.Select(async x =>
                {
                    using var conn = new SqlConnection(_connectionString);
                    var dp = ToDynamicParameters(x.Parameters);
                    var rows = await conn.QueryAsync(x.SpName, dp, commandType: CommandType.StoredProcedure);

                    return new { Name = x.SpName, Rows = rows.ToList() };
                });

                return await Task.WhenAll(tasks);
            }
            else
            {
                var results = new List<object>();

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                foreach (var proc in procedures)
                {
                    var dp = ToDynamicParameters(proc.Parameters);
                    var rows = await conn.QueryAsync(proc.SpName, dp, commandType: CommandType.StoredProcedure);

                    results.Add(new { Name = proc.SpName, Rows = rows.ToList() });
                }

                return results;
            }
        }

        public async Task<object> RunStoredProcMultiAsync(string spName,  Dictionary<string, object>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            var dp = ToDynamicParameters(parameters);

            var multi = await conn.QueryMultipleAsync(spName, dp, commandType: CommandType.StoredProcedure);

            var resultSets = new List<object>();

            while (!multi.IsConsumed)
            {
                var rows = await multi.ReadAsync();
                resultSets.Add(rows.ToList());
            }

            return new
            {
                StoredProcedure = spName,
                ResultSets = resultSets
            };
        }

    }
}
