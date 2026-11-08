// One-off: script the CaseManagement schema (no data) in dependency order.
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;

var connStr = args[0];
var outFile = args[1];

using var conn = new SqlConnection(connStr);
var server = new Server(new ServerConnection(conn));
var db = server.Databases[conn.Database] ?? throw new Exception("db not found");

var urns = new List<Urn>();
foreach (UserDefinedDataType t in db.UserDefinedDataTypes) urns.Add(t.Urn);
foreach (UserDefinedTableType t in db.UserDefinedTableTypes) urns.Add(t.Urn);
foreach (Table t in db.Tables) if (!t.IsSystemObject) urns.Add(t.Urn);
foreach (View v in db.Views) if (!v.IsSystemObject) urns.Add(v.Urn);
foreach (UserDefinedFunction f in db.UserDefinedFunctions) if (!f.IsSystemObject) urns.Add(f.Urn);
foreach (StoredProcedure p in db.StoredProcedures) if (!p.IsSystemObject) urns.Add(p.Urn);
foreach (Sequence q in db.Sequences) urns.Add(q.Urn);

var scripter = new Scripter(server)
{
    Options =
    {
        ScriptSchema = true,
        ScriptData = false,          // structure only, never rows
        WithDependencies = true,     // dependency order
        SchemaQualify = true,
        DriAll = true,
        Indexes = true,
        Triggers = true,
        ExtendedProperties = true,
        IncludeHeaders = false,
        NoCollation = true,
        TargetServerVersion = SqlServerVersion.Version160,
        IncludeIfNotExists = false,
    }
};

using var w = new StreamWriter(outFile);
w.WriteLine("-- CaseManagement schema only (no data). Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
w.WriteLine("SET NOCOUNT ON;\nGO");
using (var cn2 = new SqlConnection(connStr))
{
    cn2.Open();
    using var cmd = new SqlCommand("SELECT name FROM sys.schemas WHERE schema_id BETWEEN 5 AND 16383 ORDER BY name", cn2);
    using var rd = cmd.ExecuteReader();
    while (rd.Read()) { w.WriteLine($"CREATE SCHEMA [{rd.GetString(0)}];"); w.WriteLine("GO"); }
}
int n = 0;
foreach (var line in scripter.EnumScript(urns.ToArray()))
{
    w.WriteLine(line);
    w.WriteLine("GO");
    n++;
}
Console.WriteLine($"statements={n} objects={urns.Count}");
