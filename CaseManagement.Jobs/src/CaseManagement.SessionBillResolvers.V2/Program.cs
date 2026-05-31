using CaseManagement.SessionBillResolvers.V2;
using CaseManagement.SessionBillResolvers.V2.Engine;
using CaseManagement.SessionBillResolvers.V2.Engine.Steps;
using CaseManagement.Shared.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;
using System.Text.Json;

var jsonOptions     = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var manifestOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var caseNumberOption    = new Option<string?>("--case-number",    "Case number to process");
var sessionNumberOption = new Option<int?>  ("--session-number", "Session number within the case");
var auditFileOption     = new Option<string?>("--audit-file",    "Path to a run file (JSON with sessionDoc / projector sections)");
var actionOption        = new Option<string?>("--action",        "Which section to run: 'billingProcess' or 'projectorProcess'");
var workflowDocIdOption = new Option<int?>  ("--workflow",       "DocumentId of a workflow definition to execute");
var listOption          = new Option<bool>  ("--list",           "Print available commands as JSON and exit");
var htmlOption          = new Option<bool>  ("--html",           "With --list: output as styled HTML instead of JSON");

var rootCommand = new RootCommand("CaseManagement session billing resolver")
{
    caseNumberOption,
    sessionNumberOption,
    auditFileOption,
    actionOption,
    workflowDocIdOption,
    listOption,
    htmlOption
};

// Command manifest — add entries here as new jobs are built
var manifest = new CmdManifest("CaseManagement.Jobs", "1.0",
[
    new CmdInfo("runWorkflow", "Run Workflow",
        "Execute a named workflow by document ID. Runs all steps and saves results to DB.",
        [new CmdArg("--workflow", "int", true, "DocumentId of the workflow definition", "/api/wfRunReport?docId=")],
        "--workflow 84"),

    new CmdInfo("billingLoop", "Billing Loop",
        "Run billing for all unbilled sessions.",
        [],
        "(no args)"),

    new CmdInfo("billingCase", "Billing — Single Case",
        "Run billing for one specific case.",
        [new CmdArg("--case-number", "string", true, "Case number to process")],
        "--case-number CASE123"),

    new CmdInfo("projectorProcess", "Projector Process",
        "Run projector transform from an audit file.",
        [
            new CmdArg("--audit-file", "string", true, "Path to run file (JSON)"),
            new CmdArg("--action",     "string", true, "'projectorProcess' or 'billingProcess'")
        ],
        "--audit-file C:\\temp\\run.json --action projectorProcess"),
]);

BillingRunOptions? runOptions            = null;
RunInput?          selectedInput         = null;
string?            auditOutputDir        = null;
int?               selectedWorkflowDocId = null;
bool               saveManifestHtml      = false;

rootCommand.SetHandler((caseNumber, sessionNumber, auditFile, action, workflowDocId, list, html) =>
{
    if (list)
    {
        if (!html) { Console.WriteLine(JsonSerializer.Serialize(manifest, manifestOptions)); return; }
        saveManifestHtml = true;
        return;
    }

    if (workflowDocId is not null)
    {
        selectedWorkflowDocId = workflowDocId;
        runOptions = new BillingRunOptions(BillingRunMode.SingleRun);
        return;
    }

    if (auditFile is not null)
    {
        var json    = File.ReadAllText(auditFile);
        var runFile = JsonSerializer.Deserialize<RunFile>(json, jsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize {auditFile}");

        selectedInput = action switch
        {
            "billingProcess"   => runFile.SessionDoc,
            "projectorProcess" => runFile.Projector,
            _ => throw new InvalidOperationException(
                $"--action must be 'billingProcess' or 'projectorProcess', got '{action}'")
        } ?? throw new InvalidOperationException(
            $"Section for action '{action}' not found in {auditFile}");

        var auditCaseNumber    = selectedInput.CaseNumber   is > 0 ? selectedInput.CaseNumber.ToString() : null;
        var auditSessionNumber = selectedInput.SessionNumber is > 0 ? selectedInput.SessionNumber         : null;

        auditOutputDir = Path.GetDirectoryName(Path.GetFullPath(auditFile)) ?? @"c:\temp";
        runOptions = new BillingRunOptions(BillingRunMode.SingleRun, auditCaseNumber, auditSessionNumber, selectedInput);
        return;
    }

    var mode = caseNumber != null || sessionNumber != null
        ? BillingRunMode.SingleRun
        : BillingRunMode.Loop;

    runOptions = new BillingRunOptions(mode, caseNumber, sessionNumber);
}, caseNumberOption, sessionNumberOption, auditFileOption, actionOption, workflowDocIdOption, listOption, htmlOption);

await rootCommand.InvokeAsync(args);

if (saveManifestHtml)
{
    var manifestBuilder = Host.CreateApplicationBuilder(args);
    manifestBuilder.AddSharedInfrastructure();
    manifestBuilder.Services.AddSingleton<ICaseManagementRepository, SqlCaseManagementRepository>();
    var manifestHost = manifestBuilder.Build();
    var repo  = manifestHost.Services.GetRequiredService<ICaseManagementRepository>();
    var ct    = CancellationToken.None;

    var existing = await repo.GetDocumentAsync(new DocumentContext(DocumentType: "jobs-manifest"), ct);
    var html     = RenderManifestHtml(manifest);
    var docId    = await repo.SaveDocumentAsync(
        new DocumentContext(DocumentId: existing?.DocumentId),
        html, "jobs-manifest", "jobs-manifest.html", "text/html", ct);

    Console.WriteLine($"Saved  → docId: {docId}");
    Console.WriteLine($"Open:    http://localhost:5173/docviewer/{docId}");
    return;
}

if (runOptions is null) return;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSharedInfrastructure();

builder.Services.AddSingleton(runOptions);
builder.Services.AddSingleton<BillingProcessor>();
builder.Services.AddSingleton<IBillingCalculator, BillingCalculator>();
builder.Services.AddSingleton<ICaseManagementRepository, SqlCaseManagementRepository>();
builder.Services.AddSingleton<ProjectProcessor>();
builder.Services.AddSingleton<IWorkflowStep, ProjectorComparerStep>();
builder.Services.AddSingleton<IWorkflowStep, ProjectorStep>();
builder.Services.AddSingleton<IWorkflowStep, BillingRuleStep>();
builder.Services.AddSingleton<WorkflowEngine>();

var host = builder.Build();

Log.Information("Action: {Action} | RunId: {RunId}",
    selectedInput?.RunAction ?? "loop",
    selectedInput?.RunId     ?? "-");

if (selectedWorkflowDocId is not null)
{
    var engine = host.Services.GetRequiredService<WorkflowEngine>();
    await engine.RunAsync(selectedWorkflowDocId.Value, CancellationToken.None);
    return;
}

if (selectedInput?.RunAction == "projectorProcess")
{
    var outputDir = auditOutputDir ?? @"c:\temp";
    var projector = host.Services.GetRequiredService<ProjectProcessor>();
    await projector.RunAsync(selectedInput, outputDir, CancellationToken.None);
    return;
}

var processor = host.Services.GetRequiredService<BillingProcessor>();
await processor.RunAsync(runOptions, CancellationToken.None);

// ── Local functions ───────────────────────────────────────────────────────

static string RenderManifestHtml(CmdManifest m)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8"/>
          <title>{{m.App}} — Commands</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: Consolas, monospace; background: #f0f2f5; margin: 0; padding: 2rem; color: #222; }
            h1 { font-size: 1.2rem; margin: 0 0 0.25rem; }
            .meta { font-size: 0.8rem; color: #888; margin-bottom: 2rem; }
            table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
            th { background: #1e293b; color: #e2e8f0; text-align: left; padding: 0.6rem 1rem; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; }
            td { padding: 0.65rem 1rem; font-size: 0.82rem; border-bottom: 1px solid #f1f5f9; vertical-align: top; }
            tr:last-child td { border-bottom: none; }
            tr:hover td { background: #f8fafc; }
            .id { font-weight: bold; color: #1d4ed8; }
            .desc { color: #555; }
            .arg { display: inline-block; background: #f1f5f9; border: 1px solid #e2e8f0; border-radius: 4px; padding: 0.15rem 0.45rem; margin: 0.1rem 0.1rem 0.1rem 0; font-size: 0.75rem; }
            .arg .req { color: #dc2626; font-weight: bold; margin-left: 0.25rem; }
            code { background: #1e293b; color: #86efac; padding: 0.2rem 0.5rem; border-radius: 4px; font-size: 0.75rem; }
            .try-wrap { display: flex; flex-direction: column; gap: 0.4rem; }
            .try-row { display: flex; align-items: center; gap: 0.4rem; }
            .try-lbl { font-size: 0.7rem; color: #888; min-width: 5rem; }
            .try-input { border: 1px solid #cbd5e1; border-radius: 4px; padding: 0.2rem 0.4rem; font-size: 0.75rem; font-family: inherit; width: 5.5rem; }
            .try-input:focus { outline: none; border-color: #3b82f6; }
            .try-btn { background: #1d4ed8; color: #fff; border: none; border-radius: 4px; padding: 0.2rem 0.6rem; font-size: 0.72rem; cursor: pointer; font-family: inherit; white-space: nowrap; }
            .try-btn:hover { background: #1e40af; }
          </style>
        </head>
        <body>
          <h1>{{m.App}}</h1>
          <div class="meta">v{{m.Version}} &nbsp;·&nbsp; {{m.Commands.Length}} commands</div>
          <table>
            <thead>
              <tr><th>Command</th><th>Description</th><th>Args</th><th>Example</th><th>Try</th></tr>
            </thead>
            <tbody>
        """);

    foreach (var cmd in m.Commands)
    {
        var args = cmd.Args.Length == 0
            ? "<span style='color:#aaa'>—</span>"
            : string.Concat(cmd.Args.Select(a =>
                $"<span class='arg'>{System.Net.WebUtility.HtmlEncode(a.Name)}" +
                $"{(a.Required ? "<span class='req'>*</span>" : "")}</span>"));

        var tryArgs = cmd.Args.Where(a => a.TryUrl is not null).ToArray();
        string tryCell;
        if (tryArgs.Length == 0)
        {
            tryCell = "<span style='color:#aaa'>—</span>";
        }
        else
        {
            var tp = new System.Text.StringBuilder("<div class='try-wrap'>");
            foreach (var a in tryArgs)
            {
                var slug    = a.Name.TrimStart('-').Replace('-', '_');
                var inputId = $"inp_{cmd.Id}_{slug}";
                tp.Append($"""
                    <div class='try-row'>
                      <span class='try-lbl'>{System.Net.WebUtility.HtmlEncode(a.Name)}</span>
                      <input id='{inputId}' class='try-input' type='text' placeholder='id' />
                      <button class='try-btn' data-try='{System.Net.WebUtility.HtmlEncode(a.TryUrl)}' data-input='{inputId}'>Open ↗</button>
                    </div>
                    """);
            }
            tp.Append("</div>");
            tryCell = tp.ToString();
        }

        sb.AppendLine($"""
                  <tr>
                    <td class="id">{System.Net.WebUtility.HtmlEncode(cmd.Id)}</td>
                    <td class="desc">{System.Net.WebUtility.HtmlEncode(cmd.Description)}</td>
                    <td>{args}</td>
                    <td><code>dotnet run -- {System.Net.WebUtility.HtmlEncode(cmd.Example)}</code></td>
                    <td>{tryCell}</td>
                  </tr>
            """);
    }

    sb.AppendLine("""
            </tbody>
          </table>
          <script>
          document.querySelectorAll('.try-btn').forEach(btn => {
            const input = document.getElementById(btn.dataset.input);
            btn.addEventListener('click', () => {
              const val = input.value.trim();
              if (!val) { input.focus(); input.style.borderColor = '#ef4444'; return; }
              input.style.borderColor = '';
              window.open(btn.dataset.try + encodeURIComponent(val), '_blank');
            });
            input.addEventListener('keydown', e => { if (e.key === 'Enter') btn.click(); });
            input.addEventListener('input',   () => { input.style.borderColor = ''; });
          });
          </script>
        </body>
        </html>
        """);

    return sb.ToString();
}

// ── Type declarations (must follow all top-level statements) ──────────────

record CmdArg(string Name, string Type, bool Required, string Description, string? TryUrl = null);
record CmdInfo(string Id, string Label, string Description, CmdArg[] Args, string Example);
record CmdManifest(string App, string Version, CmdInfo[] Commands);
