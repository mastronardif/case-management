-- Enriches a just-inserted row by reading field values from a JSON document,
-- driven by the mapping registered in cases.TableFieldMap for the given table.
-- Called by usp_CaseTable_Resolve after INSERT. Silent no-op if no mapping exists.

-- exec [cases].[usp_TableFieldMap_Apply] @TableName = 'Session', @RowId = 42, @JsonDocId = 418

CREATE OR ALTER PROCEDURE [cases].[usp_TableFieldMap_Apply]
(
    @TableName  SYSNAME,
    @RowId      INT,
    @JsonDocId  INT
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Look up latest mapping for this table
    DECLARE @MappingDocId INT;
    SELECT TOP 1 @MappingDocId = JsonDocId
    FROM   [cases].[TableFieldMap]
    WHERE  TableName = @TableName
    ORDER  BY Version DESC;

    IF @MappingDocId IS NULL RETURN;

    DECLARE @mappingContent VARCHAR(MAX);
    SELECT @mappingContent = CAST(FileData AS VARCHAR(MAX))
    FROM   [cases].[Document]
    WHERE  DocumentId = @MappingDocId AND IsActive = 1;

    IF @mappingContent IS NULL RETURN;

    DECLARE @srcContent VARCHAR(MAX);
    SELECT @srcContent = CAST(FileData AS VARCHAR(MAX))
    FROM   [cases].[Document]
    WHERE  DocumentId = @JsonDocId AND IsActive = 1;

    IF @srcContent IS NULL RETURN;

    DECLARE @tbl        NVARCHAR(258)  = N'[cases].' + QUOTENAME(@TableName);
    DECLARE @setClauses NVARCHAR(MAX)  = N'';
    DECLARE @col        NVARCHAR(128);
    DECLARE @path       NVARCHAR(512);
    DECLARE @datatype   NVARCHAR(50);
    DECLARE @colExpr    NVARCHAR(MAX);

    DECLARE fieldCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            JSON_VALUE(value, '$.column'),
            JSON_VALUE(value, '$.path'),
            JSON_VALUE(value, '$.datatype')
        FROM OPENJSON(@mappingContent, '$.fields');

    OPEN fieldCursor;
    FETCH NEXT FROM fieldCursor INTO @col, @path, @datatype;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Whitelist: reject unknown datatypes to prevent injection
        IF @datatype IS NOT NULL AND @datatype NOT IN (
            'date', 'datetime', 'datetime2', 'time',
            'int', 'bigint', 'smallint', 'tinyint',
            'decimal(10,2)', 'decimal(18,2)', 'decimal(18,4)',
            'float', 'bit',
            'nvarchar', 'nvarchar(max)'
        )
        BEGIN
            FETCH NEXT FROM fieldCursor INTO @col, @path, @datatype;
            CONTINUE;
        END

        DECLARE @pathEscaped NVARCHAR(512) = REPLACE(@path, '''', '''''');

        SET @colExpr = CASE
            WHEN @datatype IS NOT NULL
                THEN 'TRY_CAST(JSON_VALUE(@src, ''' + @pathEscaped + ''') AS ' + @datatype + ')'
            ELSE
                'JSON_VALUE(@src, ''' + @pathEscaped + ''')'
        END;

        IF LEN(@setClauses) > 0 SET @setClauses += N', ';
        SET @setClauses += QUOTENAME(@col) + N' = ' + @colExpr;

        FETCH NEXT FROM fieldCursor INTO @col, @path, @datatype;
    END

    CLOSE fieldCursor;
    DEALLOCATE fieldCursor;

    IF LEN(@setClauses) = 0 RETURN;

    DECLARE @sql NVARCHAR(MAX) =
        N'UPDATE ' + @tbl + N' SET ' + @setClauses + N' WHERE Id = @RowId';

    EXEC sp_executesql @sql,
        N'@src VARCHAR(MAX), @RowId INT',
        @src = @srcContent, @RowId = @RowId;
END
GO
