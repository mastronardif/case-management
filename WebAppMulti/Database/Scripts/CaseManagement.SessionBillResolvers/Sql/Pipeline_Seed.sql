-- Run Pipeline_CreateTable.sql first

INSERT INTO [cases].[Pipeline] (Name, Description, TemplateJson, ParamsSchema, CreatedBy)
VALUES (
    'reportPipeline',
    'Gathers documents for a case and zips them into a report package.',
    '{
  "workflowId": "reportPipeline",
  "version": 1,
  "params": { "caseId": {caseId} },
  "steps": [
    {
      "id": "zip",
      "operator": "zip",
      "input": [{docIds}],
      "output": ["report.zip"]
    }
  ]
}',
    '[
  {"name":"caseId","type":"int","required":true,"label":"Case ID"},
  {"name":"docIds","type":"raw","required":true,"label":"Doc IDs (e.g. 39, 40)"}
]',
    'system'
);

INSERT INTO [cases].[Pipeline] (Name, Description, TemplateJson, ParamsSchema, CreatedBy)
VALUES (
    'billingPipeline837',
    'Runs the full 837 billing pipeline: session comparison, projection, and billing rule evaluation.',
    '{
  "workflowId": "billingPipeline837",
  "version": 1,
  "steps": [
    {
      "id": "compare",
      "operator": "projectorComparer",
      "input": [
        "{sessionDocId} sessionExtraction.json",
        "{projectionDefinitionDocId} projectionDefinition.json"
      ],
      "output": ["sessionAudit.json", "sessionReview.html"]
    },
    {
      "id": "project",
      "operator": "projector",
      "input": [
        "{sessionDocId} sessionExtraction.json",
        "{projectionDefinitionDocId} projectionDefinition.json"
      ],
      "output": ["billingProjection.json"]
    },
    {
      "id": "bill",
      "operator": "billingRule",
      "input": [
        "D2 billingProjection.json",
        "{billingRuleDocId} billingRule837"
      ],
      "output": ["billingResult.json"]
    }
  ]
}',
    '[
  {"name":"sessionDocId",              "type":"int","required":true,"label":"Session Extraction Doc ID"},
  {"name":"projectionDefinitionDocId", "type":"int","required":true,"label":"Projection Definition Doc ID"},
  {"name":"billingRuleDocId",          "type":"int","required":true,"label":"Billing Rule Doc ID"}
]',
    'system'
);
INSERT INTO [cases].[Pipeline] (Name, Description, TemplateJson, ParamsSchema, CreatedBy)
VALUES (
    '837PPipeline',
    'Builds a 837P claim JSON and HTML review for a case/session. Calls usp_Get837P and assembles all data segments.',
    '{
  "workflowId": "837PPipeline",
  "version": "1.0",
  "params": { "caseId": {caseId}, "sessionId": {sessionId} },
  "steps": [
    {
      "id": "buildClaim",
      "operator": "claim837P",
      "input": [],
      "output": ["claim837P.json", "claim837P-review.html"]
    }
  ]
}',
    '[
  {"name":"caseId",    "type":"int","required":true,"label":"Case ID"},
  {"name":"sessionId", "type":"int","required":true,"label":"Session ID"}
]',
    'system'
);
INSERT INTO [cases].[Pipeline] (Name, Description, TemplateJson, ParamsSchema, CreatedBy)
VALUES (
    'docValidate',
    'Validate an uploaded JSON doc against projection rules. Produces comparison.json + review.html for human review.',
    '{
  "workflowId": "docValidate",
  "version": "1.0",
  "params": { "caseId": {caseId} },
  "steps": [
    {
      "id": "validate",
      "operator": "projectorComparer",
      "input": ["{docId} source.json", "{ruleDocId} projectionRule.json"],
      "output": ["comparison.json", "review.html"]
    }
  ]
}',
    '[
  {"name":"docId",    "type":"int","required":true,"label":"Uploaded Doc ID"},
  {"name":"ruleDocId","type":"int","required":true,"label":"Projection Rule Doc ID"},
  {"name":"caseId",   "type":"int","required":true,"label":"Case ID"}
]',
    'system'
);

INSERT INTO [cases].[Pipeline] (Name, Description, TemplateJson, ParamsSchema, CreatedBy)
VALUES (
    'docContextPack',
    'Bundle source doc + projection rule + past examples into a ZIP for AI-assisted JSON extraction. Download the ZIP, feed the folder to AI, get the JSON back.',
    '{
  "workflowId": "docContextPack",
  "version": "1.0",
  "params": { "caseId": {caseId} },
  "steps": [
    {
      "id": "pack",
      "operator": "zip",
      "input": [{contextDocIds}],
      "output": ["context-pack.zip"]
    }
  ]
}',
    '[
  {"name":"caseId",        "type":"int","required":true, "label":"Case ID"},
  {"name":"contextDocIds", "type":"raw","required":true, "label":"Doc IDs — source doc, rule, examples (e.g. 164, 206, 120)"}
]',
    'system'
);

INSERT INTO [cases].[Pipeline] (Name, Description, TemplateJson, ParamsSchema, CreatedBy)
VALUES (
    'docResolve_Assessment',
    'Resolve an uploaded assessment JSON doc into cases.Assessment. Copy and rename for other doc types.',
    '{
  "workflowId": "docResolve_Assessment",
  "version": "1.0",
  "params": {
    "spName": "usp_Assessment_Resolve",
    "caseId": {caseId},
    "srcDocId": {srcDocId}
  },
  "steps": [
    {
      "id": "resolve",
      "operator": "docResolve",
      "input": ["{docId} source.json"],
      "output": ["confirm.json"]
    }
  ]
}',
    '[
  {"name":"docId",    "type":"int","required":true, "label":"JSON Doc ID (extracted)"},
  {"name":"caseId",   "type":"int","required":true, "label":"Case ID"},
  {"name":"srcDocId", "type":"int","required":false,"label":"Source Doc ID (original PDF/scan)"}
]',
    'system'
);
GO
