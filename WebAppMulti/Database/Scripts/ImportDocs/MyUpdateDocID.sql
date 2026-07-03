-- Enable if not already on: sp_configure 'Ad Hoc Distributed Queries', 1; RECONFIGURE;

DECLARE @fileData VARBINARY(MAX);

SELECT @fileData = BulkColumn
FROM OPENROWSET(BULK 'C:\temp\operator-registry.json', SINGLE_BLOB) AS x;

DECLARE @docId INT = 652;   -- NULL = insert new doc

EXEC [cases].[usp_Document_Save]
    @DocumentType = 'operators-catalog',
    @Title        = 'operators-catalog',
    @FileName     = 'operators-catalog.json',
    @ContentType  = 'application/json',
    @FileData     = @fileData,
    @CreatedBy    = 'ssms-import',
    @DocumentId   = @docId OUTPUT;

SELECT @docId AS NewDocId;

-- If operators registry docId changed, update MyConstants:
-- UPDATE [cases].[MyConstants] SET Value = @docId WHERE [Key] = 'Operators';
