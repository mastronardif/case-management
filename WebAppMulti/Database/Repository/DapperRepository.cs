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

        // ✅ UPDATED: supports IDictionary (CORQS friendly)
        private static DynamicParameters ToDynamicParameters(IDictionary<string, object?>? parameters)
        {
            var dp = new DynamicParameters();
            if (parameters == null) return dp;

            foreach (var kvp in parameters)
            {
                var value = kvp.Value;

                if (value == null || value == DBNull.Value)
                {
                    dp.Add(kvp.Key, null);
                }
                else
                {
                    dp.Add(kvp.Key, NormalizeJsonValue(value));
                }
            }

            return dp;
        }

        // 🔹 Single Stored Procedure (Sync)
        public IEnumerable<dynamic> ExecuteStoredProcedure(
            string spName,
            IDictionary<string, object?>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);

            return conn.Query(
                spName,
                ToDynamicParameters(parameters),
                commandType: CommandType.StoredProcedure);
        }

        // 🔹 Single Stored Procedure (Async)
        public async Task<IEnumerable<dynamic>> ExecuteStoredProcedureAsync(
            string spName,
            IDictionary<string, object?>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);

            return await conn.QueryAsync(
                spName,
                ToDynamicParameters(parameters),
                commandType: CommandType.StoredProcedure);
        }

        // 🔹 Multiple Stored Procedures (Sequential)
        public async Task<List<object>> ExecuteMultipleStoredProceduresAsync(
            List<(string SpName, IDictionary<string, object?>? Parameters)> procedures)
        {
            var results = new List<object>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var proc in procedures)
            {
                var rows = await conn.QueryAsync(
                    proc.SpName,
                    ToDynamicParameters(proc.Parameters),
                    commandType: CommandType.StoredProcedure);

                results.Add(new { Name = proc.SpName, Rows = rows });
            }

            return results;
        }

        // 🔹 Multiple Stored Procedures (Parallel)
        public async Task<List<object>> ExecuteMultipleStoredProceduresParallelAsync(
            List<(string SpName, IDictionary<string, object?>? Parameters)> procedures)
        {
            var tasks = procedures.Select(async proc =>
            {
                using var conn = new SqlConnection(_connectionString);

                var rows = await conn.QueryAsync(
                    proc.SpName,
                    ToDynamicParameters(proc.Parameters),
                    commandType: CommandType.StoredProcedure);

                return new { Name = proc.SpName, Rows = rows };
            });

            var resultsArray = await Task.WhenAll(tasks);
            return resultsArray.Cast<object>().ToList();
        }

        // 🔹 Normalize values (unchanged logic)
        private static object? NormalizeJsonValue(object? value)
        {
            if (value == null)
                return null;

            if (value is JsonElement json)
            {
                switch (json.ValueKind)
                {
                    case JsonValueKind.String:
                        return json.GetString();

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
                    case JsonValueKind.Undefined:
                        return null;

                    case JsonValueKind.Object:
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                            json.GetRawText());

                        return dict == null
                            ? null
                            : string.Join(",", dict.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

                    case JsonValueKind.Array:
                        var list = json.EnumerateArray()
                            .Select(e => NormalizeJsonValue(e))
                            .Where(v => v != null)
                            .ToList();

                        return list.Count == 0
                            ? null
                            : string.Join(",", list);

                    default:
                        return json.GetRawText();
                }
            }

            return value;
        }

        public async Task<object> RunAsync(
            string spName,
            IDictionary<string, object?>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);

            var rows = await conn.QueryAsync(
                spName,
                ToDynamicParameters(parameters),
                commandType: CommandType.StoredProcedure);

            return new
            {
                StoredProcedure = spName,
                Rows = rows.ToList()
            };
        }

        public async Task<object> RunMultiResultAsync(
            string spName,
            IDictionary<string, object?>? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);

            using var multi = await conn.QueryMultipleAsync(
                spName,
                ToDynamicParameters(parameters),
                commandType: CommandType.StoredProcedure);

            var resultSets = new List<object>();

            while (!multi.IsConsumed)
                resultSets.Add(multi.Read().ToList());

            return new
            {
                StoredProcedure = spName,
                Results = resultSets
            };
        }

        public async Task<object> RunProceduresAsync(
            List<(string SpName, IDictionary<string, object?>? Parameters)> procedures,
            bool parallel = false)
        {
            if (parallel)
            {
                var tasks = procedures.Select(async x =>
                {
                    using var conn = new SqlConnection(_connectionString);

                    var rows = await conn.QueryAsync(
                        x.SpName,
                        ToDynamicParameters(x.Parameters),
                        commandType: CommandType.StoredProcedure);

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
                    var rows = await conn.QueryAsync(
                        proc.SpName,
                        ToDynamicParameters(proc.Parameters),
                        commandType: CommandType.StoredProcedure);

                    results.Add(new { Name = proc.SpName, Rows = rows.ToList() });
                }

                return results;
            }
        }

        public async Task<object> RunStoredProcMultiAsync(string spName, IDictionary<string, object?>? parameters = null)
        {
            return await RunMultiResultAsync(spName, parameters);
        }


        public async Task<IEnumerable<dynamic>> QueryAsync(
            string sql,
            object? parameters = null,
            CommandType commandType = CommandType.Text)
        {
            using var conn = new SqlConnection(_connectionString);

            object? dbParams = parameters;

            if (parameters is IDictionary<string, object?> dict)
            {
                dbParams = ToDynamicParameters(dict);
            }

            return await conn.QueryAsync(sql, dbParams, commandType: commandType);
        }
    }
}
