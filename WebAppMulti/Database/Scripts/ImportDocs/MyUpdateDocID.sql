-- Enable if not already on: sp_configure 'Ad Hoc Distributed Queries', 1; RECONFIGURE;
use CaseManagement
DECLARE @fileData VARBINARY(MAX);

SELECT @fileData = BulkColumn
FROM OPENROWSET(BULK 'C:\temp\MiAl_2026-07-11_CASP Direct Session Note 97153_bc-5m2R1bUC7R-OIWEbv0Q.pdf', SINGLE_BLOB) AS x;
--C:\temp\operator-registry.json
-- C:\Users\mastronardif\source\repos\CaseMangement\WebAppMulti\42A\CMS1500_Boxes.projection.json

DECLARE @docId INT = 1997 --NULL --652;   -- NULL = insert new doc

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
