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
GO
