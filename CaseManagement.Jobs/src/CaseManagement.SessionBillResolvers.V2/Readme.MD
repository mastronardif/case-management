
# CaseManagement.SessionBillResolvers.V2

## Pipeline

```
sessionExtraction.json   (manually transcribed from PDF/image)
    +
projectionDefinition.json
    ↓  [projectorComparer]  → sessionAudit.json   (DB)  ← eyeball this
                            → sessionReview.html   (DB)
    ↓  [projector]          → billingProjection.json (DB)
    +
billingRule837.json
    ↓  [billingRule]        → billingResult.json   (DB)

All outputs + run manifest saved to DB. DocumentType = 'Other', Title = doc name.
```

---

## Prerequisites — import docs to DB first

All inputs must exist as documents in the DB before running a workflow.
Note the DocumentId returned for each — you'll put them in the workflow JSON.

| Doc | What it is |
|-----|------------|
| sessionExtraction.json | Transcribed session note (manually created) |
| projectionDefinition.json | Field-mapping rules (source → target) |
| billingRule837.json | Expression-based billing rule (rates, units, line items) |
| workflow.json | The workflow definition itself |

---

## Workflow JSON

```json
{
  "workflowId": "billingPipeline",
  "version": 4,
  "steps": [
    {
      "id": "compare",
      "operator": "projectorComparer",
      "input": [
        "39 sessionExtraction.json",
        "40 projectionDefinition.json"
      ],
      "output": [ "sessionAudit.json", "sessionReview.html" ]
    },
    {
      "id": "project",
      "operator": "projector",
      "input": [
        "39 sessionExtraction.json",
        "40 projectionDefinition.json"
      ],
      "output": [ "billingProjection.json" ]
    },
    {
      "id": "bill",
      "operator": "billingRule",
      "input": [
        "D2 billingProjection.json",
        "41 billingRule837.json"
      ],
      "output": [ "billingResult.json" ]
    }
  ]
}
```

Replace `39`, `40`, `41` with the actual DocumentIds from your DB.
`D2` = output of step 2 (billingProjection) — resolved automatically at runtime.

---

## Command lines

**Run a workflow (primary path):**
```
dotnet run -- --workflow 57
```
Where `57` is the DocumentId of the imported workflow JSON.

**Run projectorProcess from an audit file (legacy path):**
```
dotnet run -- --audit-file "C:\temp\auditrun\myrun.json" --action projectorProcess
```

**Run billing loop (all unbilled sessions):**
```
dotnet run
```

**Run billing for one case:**
```
dotnet run -- --case-number CASE123
```

---

## Operators

| Operator | Inputs | Outputs | Purpose |
|----------|--------|---------|---------|
| `projectorComparer` | sessionDoc, projectionDef | sessionAudit.json `[0]`, sessionReview.html `[1]` | Validates session fields against projection requirements. Eyeball the HTML. |
| `projector` | sessionDoc, projectionDef | billingProjection.json `[0]` | Transforms session data into a billing projection. |
| `billingRule` | billingProjection, billingRule | billingResult.json `[0]` | Evaluates billing expressions (timediff, floor, min, arithmetic) to produce invoice. |

`D1`, `D2`, `D3` in workflow input tokens always resolve to output `[0]` (the primary/JSON doc) of that step.

---

## billingResult.json shape

```json
{
  "ruleName": "Rule837",
  "ruleVersion": "1.101",
  "validationIssues": [],
  "lineItems": {
    "caseId": null,
    "procedureCode": "97153",
    "hours": 2.0,
    "minutes": 120.0,
    "rawUnits": 8.0,
    "units": 8.0,
    "lineAmount": 520.0
  }
}
```

---

## Run manifest (workflowRun)

Saved to DB after every workflow run. Title = `workflowRun`, FileName = `{runId}.workflowRun.json`.
Contains: runId (correlation ID), workflowDocId, all step inputs/outputs with resolved doc IDs, start/end timestamps.
Use `runId` to trace any document back to the run that produced it.
