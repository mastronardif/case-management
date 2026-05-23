root = "CaseManagement"

Write-Host "Creating CaseManagement NET 9 Clean Architecture..." -ForegroundColor Green

# Root folders
$dirs = @(
    "$root/src/CaseManagement.Worker",
    "$root/src/CaseManagement.Domain/Entities",
    "$root/src/CaseManagement.Application",
    "$root/src/CaseManagement.Infrastructure",
    "$root/src/CaseManagement.Shared",
    "$root/modules/CaseManagement.SessionBillResolvers.V2"
)

foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}

# Solution
dotnet new sln -n CaseManagement -o $root

# Projects
dotnet new worker -n CaseManagement.Worker -o "$root/src/CaseManagement.Worker" -f net9.0
dotnet new classlib -n CaseManagement.Domain -o "$root/src/CaseManagement.Domain" -f net9.0
dotnet new classlib -n CaseManagement.Application -o "$root/src/CaseManagement.Application" -f net9.0
dotnet new classlib -n CaseManagement.Infrastructure -o "$root/src/CaseManagement.Infrastructure" -f net9.0
dotnet new classlib -n CaseManagement.Shared -o "$root/src/CaseManagement.Shared" -f net9.0
dotnet new classlib -n CaseManagement.SessionBillResolvers.V2 -o "$root/modules/CaseManagement.SessionBillResolvers.V2" -f net9.0

# Add to solution
dotnet sln "$root/CaseManagement.sln" add (Get-ChildItem $root -Recurse -Filter *.csproj)

# Project references
dotnet add "$root/src/CaseManagement.Application" reference "$root/src/CaseManagement.Domain"
dotnet add "$root/src/CaseManagement.Application" reference "$root/src/CaseManagement.Shared"

dotnet add "$root/src/CaseManagement.Infrastructure" reference "$root/src/CaseManagement.Application"
dotnet add "$root/src/CaseManagement.Infrastructure" reference "$root/src/CaseManagement.Domain"

dotnet add "$root/src/CaseManagement.Worker" reference "$root/src/CaseManagement.Application"
dotnet add "$root/src/CaseManagement.Worker" reference "$root/src/CaseManagement.Infrastructure"
dotnet add "$root/src/CaseManagement.Worker" reference "$root/src/CaseManagement.Shared"

dotnet add "$root/modules/CaseManagement.SessionBillResolvers.V2" reference "$root/src/CaseManagement.Application"
dotnet add "$root/modules/CaseManagement.SessionBillResolvers.V2" reference "$root/src/CaseManagement.Domain"

# Domain sample
@"
namespace CaseManagement.Domain.Entities;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}
"@ | Out-File "$root/src/CaseManagement.Domain/Entities/Case.cs"

# Application bootstrap
@"
using Microsoft.Extensions.DependencyInjection;

namespace CaseManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
"@ | Out-File "$root/src/CaseManagement.Application/DependencyInjection.cs"

# Infrastructure bootstrap
@"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CaseManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        return services;
    }
}
"@ | Out-File "$root/src/CaseManagement.Infrastructure/DependencyInjection.cs"

# Worker program override
@"
using CaseManagement.Application;
using CaseManagement.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
"@ | Out-File "$root/src/CaseManagement.Worker/Program.cs"

Write-Host "DONE: CaseManagement NET 9 solution created." -ForegroundColor Green