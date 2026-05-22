dotnet new sln -n CaseManagement

dotnet sln add .\src\CaseManagement.Shared\CaseManagement.Shared.csproj
dotnet sln add .\src\CaseManagement.Tooling\CaseManagement.Tooling.csproj
dotnet sln add .\src\CaseManagement.SessionResolvers\CaseManagement.SessionResolvers.csproj
dotnet sln add .\src\CaseManagement.SessionBillResolvers.V2\CaseManagement.SessionBillResolvers.V2.csproj
dotnet sln add .\src\CaseManagement.DocumentResolvers\CaseManagement.DocumentResolvers.csproj
