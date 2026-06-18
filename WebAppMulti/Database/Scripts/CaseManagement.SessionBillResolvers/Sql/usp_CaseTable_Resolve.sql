-- Generic INSERT for any [cases] table that has CaseId, SourceDocumentId, JsonDocumentId.
-- After insert, calls usp_TableFieldMap_Apply to hydrate mapped columns from the JSON doc.
-- Replaces per-table copies like usp_Assessment_Resolve / usp_Authorization_Resolve.

-- exec [cases].[usp_CaseTable_Resolve] @TableName = 'Session', @DocId = 418, @CaseId = 5, @SrcDocId = 370

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

    DECLARE @tbl   NVARCHAR(258) = QUOTENAME(@TableName);
    DECLARE @NewId INT;

    -- Temp table captures the inserted Id across the sp_executesql boundary.
    -- SCOPE_IDENTITY() is unreliable inside sp_executesql called from a stored procedure.
    CREATE TABLE #InsertedId (Id INT);

    DECLARE @sql NVARCHAR(MAX) = N'
    INSERT INTO [cases].' + @tbl + N' (CaseId, JsonDocumentId, SourceDocumentId)
    OUTPUT INSERTED.Id INTO #InsertedId
    VALUES (@CaseId, @DocId, @SrcDocId);';

    EXEC sp_executesql @sql,
        N'@CaseId INT, @DocId INT, @SrcDocId INT',
        @CaseId = @CaseId, @DocId = @DocId, @SrcDocId = @SrcDocId;

    SELECT @NewId = Id FROM #InsertedId;
    DROP TABLE #InsertedId;

    -- Hydrate mapped columns from JSON doc
    IF @NewId IS NOT NULL
        EXEC [cases].[usp_TableFieldMap_Apply]
            @TableName = @TableName,
            @RowId     = @NewId,
            @JsonDocId = @DocId;
END
GO
