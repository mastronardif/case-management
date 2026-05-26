using CaseManagement.SessionBillResolvers.V2;
using CaseManagement.SessionBillResolvers.V2.Engine;
using CaseManagement.Shared.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;
using System.Text.Json;

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

var caseNumberOption  = new Option<string?>("--case-number",  "Case number to process");
var sessionNumberOption = new Option<int?> ("--session-number", "Session number within the case");
var auditFileOption   = new Option<string?>("--audit-file",   "Path to a run file (JSON with sessionDoc / projector sections)");
var actionOption      = new Option<string?>("--action",       "Which section to run: 'billingProcess' or 'projectorProcess'");

var rootCommand = new RootCommand("CaseManagement session billing resolver")
{
    caseNumberOption,
    sessionNumberOption,
    auditFileOption,
    actionOption
};

BillingRunOptions? runOptions    = null;
RunInput?          selectedInput  = null;
string?            auditOutputDir = null;

rootCommand.SetHandler((caseNumber, sessionNumber, auditFile, action) =>
{
    if (auditFile is not null)
    {
        var json = File.ReadAllText(auditFile);
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
}, caseNumberOption, sessionNumberOption, auditFileOption, actionOption);

await rootCommand.InvokeAsync(args);

if (runOptions is null) return;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSharedInfrastructure();

builder.Services.AddSingleton(runOptions);
builder.Services.AddSingleton<BillingProcessor>();
builder.Services.AddSingleton<IBillingCalculator, BillingCalculator>();
builder.Services.AddSingleton<ICaseManagementRepository, SqlCaseManagementRepository>();
builder.Services.AddSingleton<ProjectProcessor>();

var host = builder.Build();

Log.Information("Action: {Action} | RunId: {RunId}",
    selectedInput?.RunAction ?? "loop",
    selectedInput?.RunId     ?? "-");

if (selectedInput?.RunAction == "projectorProcess")
{
    var outputDir = auditOutputDir ?? @"c:\temp";
    var projector = host.Services.GetRequiredService<ProjectProcessor>();
    await projector.RunAsync(selectedInput, outputDir, CancellationToken.None);
    return;
}

var processor = host.Services.GetRequiredService<BillingProcessor>();
await processor.RunAsync(runOptions, CancellationToken.None);