-- Enable if not already on: sp_configure 'Ad Hoc Distributed Queries', 1; RECONFIGURE;
use CaseManagement
DECLARE @fileData VARBINARY(MAX);

SELECT @fileData = BulkColumn
FROM OPENROWSET(BULK 'C:\temp\732.Session837P.rule.002.updated.json', SINGLE_BLOB) AS x;
--C:\temp\operator-registry.json
-- C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti\42A\CMS1500_Boxes.projection.json

DECLARE @docId INT = 732 --NULL --652;   -- NULL = insert new doc

EXEC [cases].[usp_Document_Save]
    --@DocumentType = 'operators-catalog',
    --@Title        = 'Session837P.projection',
    --@FileName     = 'Session837P.projection',
    --@ContentType  = 'application/json',
    @FileData     = @fileData,
    --@CreatedBy    = 'ssms-import',
    @DocumentId   = @docId OUTPUT;

SELECT @docId AS NewDocId;

-- If operators registry docId changed, update MyConstants:
-- UPDATE [cases].[MyConstants] SET Value = @docId WHERE [Key] = 'Operators';
