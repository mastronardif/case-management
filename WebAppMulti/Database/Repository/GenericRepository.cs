//using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace WebAppMulti.Database.Repository
{

    public class GenericRepository
    {
        private readonly string _connectionString;

        public GenericRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public DataTable ExecuteStoredProcedure(string spName, Dictionary<string, object>? parameters = null)
        {
            var dt = new DataTable();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        //cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(kvp.Key, NormalizeJsonValue(kvp.Value));
                    }
                }

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }

        public async Task<DataTable> ExecuteStoredProcedureAsync(string spName, Dictionary<string, object>? parameters = null)
        {
            var dt = new DataTable();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                        //cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(kvp.Key, NormalizeJsonValue(kvp.Value));
                }

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
            }

            return dt;
        }


        /// <summary>
        /// Execute multiple stored procedures and return a dictionary of results.
        /// Each procedure name maps to a List of Dictionary rows.
        /// </summary>
        public Dictionary<string, List<Dictionary<string, object>>> ExecuteMultipleStoredProcedures(
            List<(string SpName, Dictionary<string, object>? Parameters)> procedures)
        {
            var results = new Dictionary<string, List<Dictionary<string, object>>>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var proc in procedures)
                {
                    using (var cmd = new SqlCommand(proc.SpName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        if (proc.Parameters != null)
                        {
                            foreach (var kvp in proc.Parameters)
                            {
                                cmd.Parameters.AddWithValue(kvp.Key, NormalizeJsonValue(kvp.Value));
                            }

                            //foreach (var kvp in proc.Parameters)
                            //{
                            //    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
                            //}
                        }

                        var dt = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }

                        // Convert DataTable to List<Dictionary<string, object>>
                        var rows = dt.AsEnumerable()
                            .Select(r => dt.Columns.Cast<DataColumn>()
                                .ToDictionary(c => c.ColumnName, c => r[c]))
                            .ToList();

                        results[proc.SpName] = rows;
                    }
                }
            }

            return results;
        }

        public async Task<List<object>> ExecuteMultipleStoredProceduresAsync(
            List<(string SpName, Dictionary<string, object>? Parameters)> procedures)
        {
            var results = new List<object>(); //Dictionary<string, List<Dictionary<string, object>>>();

            await using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                foreach (var proc in procedures)
                {
                    await using (var cmd = new SqlCommand(proc.SpName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        if (proc.Parameters != null)
                        {
                            foreach (var kvp in proc.Parameters)
                            {
                                //cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
                                cmd.Parameters.AddWithValue(kvp.Key, NormalizeJsonValue(kvp.Value));
                            }
                        }

                        var dt = new DataTable();
                        await using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            dt.Load(reader);
                        }

                        var rows = dt.AsEnumerable()
                            .Select(r => dt.Columns.Cast<DataColumn>()
                                .ToDictionary(c => c.ColumnName, c => r[c]))
                            .ToList();

                        //results[proc.SpName] = rows;
                        results.Add(new { Name = proc.SpName, Rows = rows });
                    }
                }
            }

            return results;
        }

        public async Task<List<object>> ExecuteMultipleStoredProceduresParallelAsync(
            List<(string SpName, Dictionary<string, object>? Parameters)> procedures)
        {
            var tasks = procedures.Select(async proc =>
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand(proc.SpName, conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (proc.Parameters != null)
                {
                    foreach (var kvp in proc.Parameters)
                    //    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
                    cmd.Parameters.AddWithValue(kvp.Key, NormalizeJsonValue(kvp.Value));
                }

                var dt = new DataTable();
                await using var reader = await cmd.ExecuteReaderAsync();
                dt.Load(reader);

                var rows = dt.AsEnumerable()
                    .Select(r => dt.Columns.Cast<DataColumn>()
                        .ToDictionary(c => c.ColumnName, c => r[c]))
                    .ToList();

                return new { Name = proc.SpName, Rows = rows };
            });

            var resultsArray = await Task.WhenAll(tasks);
            // Flatten to a simple List<object> for easy serialization
            return resultsArray.Cast<object>().ToList();
        }


        private static object NormalizeJsonValue(object value)
        {
            if (value is null)
                return DBNull.Value;

            if (value is JsonElement json)
            {
                switch (json.ValueKind)
                {
                    case JsonValueKind.String:
                        {
                            var s = json.GetString();
                            return s is null ? DBNull.Value : s;
                        }

                    case JsonValueKind.Number:
                        if (json.TryGetInt32(out var i)) return i;
                        if (json.TryGetInt64(out var l)) return l;
                        if (json.TryGetDecimal(out var d)) return d;
                        if (json.TryGetDouble(out var dbl)) return dbl;
                        return json.GetRawText();

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return json.GetBoolean();

                    case JsonValueKind.Null:
                        return DBNull.Value;

                    case JsonValueKind.Object:
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json.GetRawText());
                            if (dict == null)
                                return DBNull.Value;

                            var normalized = dict.ToDictionary(k => k.Key, v => NormalizeJsonValue(v.Value));
                            return (object)normalized;
                        }

                    case JsonValueKind.Array:
                        {
                            var list = json.EnumerateArray()
                                           .Select(e => NormalizeJsonValue(e))
                                           .ToList();

                            // If SQL can't handle lists, serialize to string
                            return list.Count == 0 ? DBNull.Value : string.Join(",", list);
                        }

                    default:
                        return json.GetRawText();
                }
            }

            return value;
        }




    }
}