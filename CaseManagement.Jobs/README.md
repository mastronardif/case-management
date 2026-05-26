# CaseManagement ArchitectureStarter

A pluggable .NET 9 toolchain for CaseManagement backend processing. Each tool is an independently runnable worker that shares common infrastructure (logging, configuration, DB connectivity) via `CaseManagement.Shared`.

---

## Solution Structure

```
src/
├── CaseManagement.Shared/                  # Shared infrastructure (config, logging, DI bootstrap)
│   ├── appsettings.json                    # Single source of truth — connection string, Serilog sinks
│   ├── Bootstrapping/
│   │   └── SharedInfrastructureExtensions  # AddSharedInfrastructure() extension method
│   ├── Configuration/
│   │   └── AppConfiguration               # Loads appsettings.json + appsettings.local.json
│   └── Models/
│       └── ConnectionSettings             # Typed DB connection injectable via DI
│
└── CaseManagement.SessionBillResolvers.V2/ # Tool: session billing resolver
    ├── appsettings.local.json              # Tool-specific overrides (Billing settings)
    ├── Engine/
    ├── Calculators/
    ├── Providers/
    ├── Repositories/
    ├── Runner/
    └── Workers/
```

---

## Shared Infrastructure

All tools call one method in `Program.cs` to get logging, config, and DB connection wired up:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.AddSharedInfrastructure();
```

This bootstraps:
- **Serilog** — console + rolling file (`C:/temp/casemanagement-.txt`) + SQL Server `ApplicationLogs` table
- **`ConnectionSettings`** — typed DB connection string injectable into any service
- **`IConfiguration`** — full merged config available via DI

### Configuration layering

| File | Location | Purpose |
|---|---|---|
| `appsettings.json` | `CaseManagement.Shared/` | Shared — DB connection, Serilog sinks. Edit here, all tools pick it up on next build. |
| `appsettings.local.json` | Per-tool project | Tool-specific overrides (e.g. Billing batch size). Optional, not committed if sensitive. |

---

## Tools

### CaseManagement.SessionBillResolvers.V2

Resolves and persists billing invoices for unbilled sessions. Runs as a background worker.

#### Usage

```
dotnet run                                      # Loop mode — polls DB every minute
dotnet run -- --case-number 1234               # Single case, all sessions
dotnet run -- --case-number 1234 --session-number 56  # Single case, single session
dotnet run -- --help                            # Show usage
```

#### Arguments

| Argument | Type | Required | Description |
|---|---|---|---|
| `--case-number` | string | No | Restrict processing to a specific case number |
| `--session-number` | int | No | Restrict processing to a specific session within the case |

#### Run modes

**Loop mode** (no arguments)
Polls `usp_GetUnbilledSessions` on a configurable interval. Processes all unbilled sessions across all cases. Runs until stopped.

**Single-run mode** (`--case-number` and/or `--session-number` supplied)
Processes the specified case/session once, writes results, then exits cleanly.

#### Stored procedures

| SP | Parameters | Description |
|---|---|---|
| `usp_GetUnbilledSessions` | `@CaseNumber`, `@SessionNumber` (both optional) | Returns unbilled sessions. If params supplied, filters to that case/session. |
| `usp_SaveBillingInvoice` | `@SessionId`, `@PatientName`, `@Amount` | Persists a calculated invoice. |

#### Tool-specific settings (`appsettings.local.json`)

```json
{
  "Billing": {
    "BatchSize": 100,
    "Mode": "Default"
  }
}
```

---

## Adding a new tool

1. Create a new project under `src/` (classlib or worker)
2. Add `ProjectReference` to `CaseManagement.Shared`
3. Link the shared `appsettings.json` in the csproj:
```xml
<Content Include="..\CaseManagement.Shared\appsettings.json">
  <Link>appsettings.json</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```
4. Call `builder.AddSharedInfrastructure()` in `Program.cs`
5. Add an `appsettings.local.json` for any tool-specific settings