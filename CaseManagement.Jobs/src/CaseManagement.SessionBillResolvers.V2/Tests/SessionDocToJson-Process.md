# Session Doc → Session JSON — Manual Process

How a raw uploaded session document becomes a validated row in `cases.Session`. Used to
validate the JSON extracted from a document before it becomes authoritative. Currently
run by hand; candidate for a `.ps1` (mirroring `BuildInvoice837P.ps1`) or a small webpage
once the steps below are confirmed against real usage.

## Steps

1. **Add the session document to the doc database.**
   Upload the raw source file (scan/PDF/etc). Existing tool: `/api/docWorkbench`
   ([DocWorkbenchEndpoint.cs](../../../../WebAppMulti/Endpoints/DocWorkbenchEndpoint.cs)),
   which posts to `/api/uploadDocument` (`caseId`, optional `sessionId`, `documentType`, `file`)
   and returns a `docId`. This becomes the session's `SourceDocumentId`.

2. **Pack the context.** Run the `docContextPack` workflow — a `(Z)`/zip-operator DSL workflow
   that bundles the projector doc, the rule doc, and the source doc into a downloadable zip.
   Example (docId 404, built for a different case):
   ```json
   {
     "workflowId": "docContextPack",
     "params": { "caseId": 5 },
     "steps": [{ "id": "pack", "operator": "zip", "input": [370, 367, 380], "output": ["context-pack.zip"] }]
   }
   ```
   Run via `dotnet run -- --workflow <workflowDocId> --case-id <caseId>`.
   **Open question**: the `input` array is currently hardcoded per workflow doc — a new
   session means either editing this doc or creating a new one. A future script would need
   to build this input list dynamically (same pattern as `Get-ActiveProjectionDocId`/
   `Get-ActiveRuleDocId` in `BuildInvoice837P.ps1` — resolve projector/rule docIds from
   `[cases].[ProjectorRule]` by name, plus the session's own `SourceDocumentId`).

3. **Run AI** on the packed zip to produce the session note JSON. Manual/external step —
   not scriptable in `.ps1` without an AI API call, but the script could still prepare the
   zip and pause for the AI-produced file.

4. **Import the Session Note.json.** Same `/api/uploadDocument` mechanism as step 1, this
   time uploading the AI-produced JSON. Returns a new `docId` — becomes the session's
   `JsonDocumentId`.

5. **Validate, preview, and commit — one page, not two separate actions.** Run the `(V)` /
   `projectorComparer` operator to compare the imported JSON against the **projection** doc.
   This produces an HTML review page (`RenderReviewHtml` in `ProjectProcessor.cs`) with an
   editable preview of every field **and**, when the right flags are passed, a **"Save &
   Resolve" button right on that same page** that commits to `cases.Session` — there is no
   separate manual SQL step. This is the part that kept getting missed: without
   `--table-name`/`--src-doc-id`, the button silently doesn't render and it looks like commit
   must be a separate action.
   ```
   dotnet run -- --expression "<jsonDocId> (V) <projectionDocId>" --case-id <caseId> `
       --table-name Session --src-doc-id <sourceDocId>
   ```
   Example for Session837P: `dotnet run -- --expression "1986 (V) 731" --case-id 5 --table-name Session --src-doc-id 1626`.

   **Important — projection doc, not rule doc.** `(V)`'s second input must be the
   **projection** doc (e.g. 731 for Session837P — the one shaped `{fields: [{target, source}]}`),
   *not* the rule doc (732 — shaped `{ruleType, constants, requiredFields, rules}`). Passing
   the rule doc by mistake throws a `NullReferenceException` in `ProjectProcessor.Project`
   (`ProjectProcessor.cs:58`). Don't confuse this with `(Q)` (`billingRule837P`), which *does*
   consume the rule doc — that's a separate operator for calculating `billing.*` derived
   values, not part of this validate step.

   **What the page's buttons actually do:**
   - **"Save Corrected JSON"** — always present. Collects any hand-edited values from the
     preview inputs and `POST /api/saveDocument` → new `JsonDocumentId`.
   - **"Save & Resolve"** — only rendered when `tableName`/`caseId` are present (i.e. you
     passed `--table-name`/`--case-id`). Saves (same as above), then
     `POST /api/resolveDoc { docId, tableName, caseId, srcDocId }`
     ([ResolveDocEndpoint.cs](../../../../WebAppMulti/Endpoints/ResolveDocEndpoint.cs)), which
     calls `usp_CaseTable_Resolve` server-side — the actual commit into `cases.Session`.

   The raw SQL, for reference (this is what the button runs, not something you run by hand):
   ```sql
   EXEC [cases].[usp_CaseTable_Resolve]
       @TableName = 'Session',
       @DocId     = <jsonDocId>,     -- from the "Save" half of Save & Resolve
       @CaseId    = <caseId>,
       @SrcDocId  = <sourceDocId>;   -- from step 1
   ```
   ([usp_CaseTable_Resolve.sql](../../../../WebAppMulti/Database/Scripts/CaseManagement.SessionBillResolvers/Sql/usp_CaseTable_Resolve.sql))
   Generic resolver for any `[cases]` table with `CaseId`/`SourceDocumentId`/`JsonDocumentId`
   columns — inserts the row, then calls `usp_TableFieldMap_Apply` to hydrate the mapped
   columns from the JSON doc.

## Confirmed vs. open

| Step | Mechanism | Confidence |
|---|---|---|
| 1. Add source doc | `/api/docWorkbench` → `/api/uploadDocument` | Confirmed (read the endpoint source) |
| 2. Pack context | `docContextPack` workflow, `(Z)` zip operator | Confirmed shape; dynamic input list is open |
| 3. AI extraction | External, manual | N/A — not scriptable as-is |
| 4. Import JSON | `/api/uploadDocument` (same as step 1) | Confirmed |
| 5–6. Validate + commit | `(V)` with `--table-name`/`--src-doc-id`, then "Save & Resolve" button → `/api/resolveDoc` → `usp_CaseTable_Resolve` | Confirmed against `ProjectorComparerStep.cs`/`ProjectProcessor.cs`/`ResolveDocEndpoint.cs`; one page, not two steps |
