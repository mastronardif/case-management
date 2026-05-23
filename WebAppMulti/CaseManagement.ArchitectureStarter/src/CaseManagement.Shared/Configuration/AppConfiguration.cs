using Microsoft.Extensions.Configuration;

namespace CaseManagement.Shared.Configuration;

public static class AppConfiguration
{
    public static IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }
}