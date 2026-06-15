-- Generic resolve/upsert for any [cases] table that has CaseId, SourceDocumentId, JsonDocumentId.
-- Replaces per-table copies like usp_Assessment_Resolve / usp_Authorization_Resolve.
-- Called by DocResolveStep via ResolveDocAsync.

-- exec [cases].[usp_CaseTable_Resolve] @TableName = 'Assessment', @DocId = 224, @CaseId = 7, @SrcDocId = 164
-- exec [cases].[usp_CaseTable_Resolve] @TableName = 'Authorization', @DocId = 329, @CaseId = 12, @SrcDocId = 291

CREATE OR ALTER PROCEDURE [cases].[usp_CaseTable_Resolve]
(
    @TableName SYSNAME,
    @DocId     INT,
    @CaseId    INT,
    @SrcDocId  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Whitelist: table must exist in [cases] and expose the three resolve columns
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'cases'
          AND TABLE_NAME   = @TableName
          AND COLUMN_NAME IN ('CaseId', 'SourceDocumentId', 'JsonDocumentId')
        GROUP BY TABLE_NAME
        HAVING COUNT(DISTINCT COLUMN_NAME) = 3
    )
        THROW 50002, 'Table is not a valid resolve target (missing CaseId/SourceDocumentId/JsonDocumentId).', 1;

    IF NOT EXISTS (SELECT 1 FROM [cases].[Document] WHERE DocumentId = @DocId AND IsActive = 1)
        THROW 50001, 'JSON document not found or inactive.', 1;

    DECLARE @tbl NVARCHAR(258) = QUOTENAME(@TableName);
    DECLARE @sql NVARCHAR(MAX) = N'
IF EXISTS (SELECT 1 FROM [cases].' + @tbl + N' WHERE CaseId = @CaseId)
BEGIN
    UPDATE [cases].' + @tbl + N'
    SET JsonDocumentId   = @DocId,
        SourceDocumentId = @SrcDocId
    WHERE CaseId = @CaseId;
END
ELSE
BEGIN
    INSERT INTO [cases].' + @tbl + N' (CaseId, JsonDocumentId, SourceDocumentId)
    VALUES (@CaseId, @DocId, @SrcDocId);
END';

    EXEC sp_executesql @sql,
        N'@CaseId INT, @DocId INT, @SrcDocId INT',
        @CaseId = @CaseId, @DocId = @DocId, @SrcDocId = @SrcDocId;
END
GO
