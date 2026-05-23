using CaseManagement.SessionBillResolvers.V2;
using CaseManagement.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var config = AppConfiguration.Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing from appsettings.json.");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// --------------------
// Config binding
// --------------------
builder.Services.AddSingleton<BillingSettings>(_ => new BillingSettings
{
    BatchSize = config.GetValue<int>("Billing:BatchSize", 100),
    Mode = config.GetValue<string>("Billing:Mode") ?? "Default"
});

// --------------------
// Core DI
// --------------------
builder.Services.AddSingleton<BillingRunner>();
builder.Services.AddSingleton<BillingEngine>();

builder.Services.AddSingleton<ISessionProvider>(sp =>
    new SessionProvider(connectionString, sp.GetRequiredService<ILogger<SessionProvider>>()));

builder.Services.AddSingleton<IBillingCalculator, BillingCalculator>();
builder.Services.AddSingleton<IBillingRepository>(_ => new SqlBillingRepository(connectionString));

builder.Services.AddHostedService<BillingWorker>();

var host = builder.Build();

Log.Information("CaseManagement.SessionBillResolvers.V2 started");

await host.RunAsync();