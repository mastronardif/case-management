using CaseManagement.SessionBillResolvers.V2;
using CaseManagement.Shared.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;

var caseNumberOption = new Option<string?>("--case-number", "Case number to process");
var sessionNumberOption = new Option<int?>("--session-number", "Session number within the case");

var rootCommand = new RootCommand("CaseManagement session billing resolver")
{
    caseNumberOption,
    sessionNumberOption
};

BillingRunOptions? runOptions = null;

rootCommand.SetHandler((caseNumber, sessionNumber) =>
{
    var mode = caseNumber != null || sessionNumber != null
        ? BillingRunMode.SingleRun
        : BillingRunMode.Loop;

    runOptions = new BillingRunOptions(mode, caseNumber, sessionNumber);
}, caseNumberOption, sessionNumberOption);

await rootCommand.InvokeAsync(args);

if (runOptions is null) return;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSharedInfrastructure();

builder.Services.AddSingleton(runOptions);
builder.Services.AddSingleton<BillingProcessor>();
builder.Services.AddSingleton<ISessionProvider, SessionProvider>();
builder.Services.AddSingleton<IBillingCalculator, BillingCalculator>();
builder.Services.AddSingleton<IBillingRepository, SqlBillingRepository>();

var host = builder.Build();
Log.Information("CaseManagement.SessionBillResolvers.V2 started. Mode: {Mode}", runOptions.Mode);

var processor = host.Services.GetRequiredService<BillingProcessor>();
await processor.RunAsync(runOptions, CancellationToken.None);