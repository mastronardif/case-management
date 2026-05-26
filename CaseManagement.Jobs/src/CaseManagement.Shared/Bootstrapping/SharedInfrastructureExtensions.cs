using CaseManagement.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CaseManagement.Shared.Bootstrapping;

public static class SharedInfrastructureExtensions
{
    public static IHostApplicationBuilder AddSharedInfrastructure(
        this IHostApplicationBuilder builder)
    {
        var config = AppConfiguration.Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing from appsettings.json.");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(new ConnectionSettings(connectionString));

        return builder;
    }
}