# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Layout

Three independently runnable projects:

- **CaseManagementUI/** — React 19 + Vite + Tailwind frontend
- **WebAppMulti/** — .NET 9 ASP.NET Core backend API
- **CaseManagement.Jobs/** — Pluggable .NET 9 background worker toolchain for billing/document processing
- **Payer/JsonTo837/** — EDI 837 claims conversion utility

## Commands

### Frontend (CaseManagementUI)
```bash
npm install
npm run dev       # Dev server; proxies /api/* to backend
npm run build
npm run lint
```

### Backend (WebAppMulti)
```bash
dotnet build
dotnet run        # Starts on HTTPS port 44344 by default
```

### Background Workers (CaseManagement.Jobs)
Each tool under `CaseManagement.Jobs/src/` runs independently:
```bash
dotnet run                                              # Polling/loop mode
dotnet run -- --case-number 1234                        # Single case
dotnet run -- --case-number 1234 --session-number 56   # Single session
dotnet run -- --help
```

## Architecture

### Data flow
`Sessions → Invoices → 837 EDI Claims`

SQL Server is the system of record. Full audit trail is required on all billing operations. Stored procedures live in `WebAppMulti/Database/Scripts/` and cover the core billing pipeline (`usp_GetUnbilledSessions`, `usp_SaveBillingInvoice`, `usp_Session_GetBillingContext`).

### CORQS — Schema-Driven API System
The primary backend pattern. `WebAppMulti/Database/Schema/schema.json` declares all API operations at startup. `SchemaRegistry` loads this file, and `CorqsExecutor` routes each request through a strategy (stored procedure, MediatR handler, raw SQL, or HTTP). This generates OpenAPI docs, Postman collections, and the React `QUERY_MAP` automatically. Adding a new operation means editing `schema.json`, not writing a new controller.

Row action buttons on `DataTable22` are also declared in `schema.json` and rendered from `QUERY_MAP` — do not hardcode them in the component.

### Backend layout (WebAppMulti)
```
Controllers/         # Legacy REST controllers
Endpoints/           # Minimal API endpoints (preferred for new code)
Modules/Cases/       # Feature modules: Commands, Queries, Domain, Sql subdirs
Services/
  Corqs/             # Execution engine and strategies
  SchemaService/     # Metadata registry
  CaseManagement/    # Domain services (sessions, documents, form templates)
Database/
  Models/            # EF Core entities
  Dtos/              # DTOs
  Repository/        # GenericRepository (EF), DapperRepository (Dapper)
  Schema/schema.json # Single source of truth for API surface
```

Data access uses EF Core 9 for entities and Dapper for complex queries/SPs. Both coexist; prefer Dapper for anything involving stored procedures.

### Frontend layout (CaseManagementUI/src)
```
App.jsx         # Root layout: Sidebar + Header + Footer + React Router outlet
routes.jsx      # All route definitions
pages/          # One component per route
components/     # Shared UI (DataTable22, Calendar, etc.)
services/       # apiFetch, billingService, fileService
context/        # AuthContext (JWT), GlobalContext
```

### Reports / Documents
Reports and generated documents are self-contained HTML files. They are saved to the document database and served via `GET /api/getDocument`. Do not wrap them in React components. Fetch them via `Invoke-RestMethod` against `localhost` when testing locally.

### Background Jobs
`CaseManagement.Shared/` provides shared infrastructure (DB connection, config, logging). Each tool project adds an `appsettings.local.json` for tool-specific overrides (e.g., `Billing.BatchSize`, `Billing.Mode`). Serilog logs to console, rolling files at `C:/temp/casemanagement-.txt`, and the `ApplicationLogs` SQL table.

## Key Configuration

| File | Purpose |
|---|---|
| `WebAppMulti/appsettings.json` | SQL connection string, JWT key, Serilog sinks |
| `CaseManagementUI/.env.development` | `VITE_API_BASE_URL=https://localhost:7009/api` |
| `CaseManagement.Jobs/src/CaseManagement.Shared/appsettings.json` | Shared worker config |
| `WebAppMulti/Database/Schema/schema.json` | Full API surface definition |
| `CaseManagementUI/vite.config.js` | Dev proxy (`/api` → backend) |
| `WebAppMulti/Program.cs` | DI registration, middleware, Swagger setup |

The JWT secret in `appsettings.json` is a placeholder (`super_secret_long_key_change_this`) — not production-ready. `DummyAuthStore.cs` is also dev-only.

Dev SQL Server: `LAPTOP-JIH94VS9\SQLEXPRESS`, database `CaseManagement`, Windows trusted auth.
