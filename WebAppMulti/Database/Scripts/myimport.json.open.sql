
Option 1 — JSON file is an array of objects

Example JSON:
[
  { "id": 1, "name": "Frank", "dept": "IT" },
  { "id": 2, "name": "Sue", "dept": "HR" }
]


Step 1 — Load JSON into a NVARCHAR(MAX) variable
DECLARE @json NVARCHAR(MAX);

SELECT @json = BulkColumn
FROM OPENROWSET(
        BULK 'C:\Path\forms_metadata.json',
        SINGLE_CLOB
    ) AS j;


Step 2 — Create a real table
CREATE TABLE dbo.JsonImport (
    id INT,
    name NVARCHAR(200),
    dept NVARCHAR(200)
);

Step 3 — Insert JSON into the table
INSERT INTO dbo.JsonImport (id, name, dept)
SELECT *
FROM OPENJSON(@json)
WITH (
    id INT,
    name NVARCHAR(200),
    dept NVARCHAR(200)
);
