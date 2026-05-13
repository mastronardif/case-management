CREATE PROCEDURE [cases].[usp_Workbook_InitializeForCase]
    @CaseId INT,
    @CreatedBy NVARCHAR(100)
	--EXEC [cases].[usp_Workbook_InitializeForCase]  @CaseId = 1, @CreatedBy = 'system'
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH SourceTypes AS
    (
        SELECT
            WorkbookTypeId,
            Name
        FROM [cases].[WorkbookType]
        WHERE IsActive = 1
    )
    INSERT INTO [cases].[Workbook]
    (
        CaseId,
        WorkbookTypeId,
        Status,
        CreatedBy,
        IsActive
    )
    SELECT
        @CaseId,
        st.WorkbookTypeId,
        'Pending',
        @CreatedBy,
        1
    FROM SourceTypes st
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [cases].[Workbook] w
        WHERE w.CaseId = @CaseId
          AND w.WorkbookTypeId = st.WorkbookTypeId
    );
END;
