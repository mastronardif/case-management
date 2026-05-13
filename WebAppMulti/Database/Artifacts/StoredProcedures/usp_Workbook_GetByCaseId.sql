CREATE   PROCEDURE [cases].[usp_Workbook_GetByCaseId]
    @CaseId INT
	--EXEC [cases].[usp_Workbook_GetByCaseId]  @CaseId = 1001
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wt.WorkbookTypeId,
        wt.Name AS WorkbookName,
        wt.Description,
        wt.DisplayOrder,

        w.WorkbookId,
        --w.CaseId,
		COALESCE(w.CaseId, 2) AS CaseId,
		'session.187.aba_session_editable.pdf' as fileName,
        w.Status,
        w.DueDate,
        w.CreatedDate,
        w.CompletedDate,

        CAST(CASE WHEN w.WorkbookId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsStarted,
        CAST(CASE WHEN ISNULL(doc.DocumentCount, 0) > 0 THEN 1 ELSE 0 END AS BIT) AS HasDocuments,
        ISNULL(doc.DocumentCount, 0) AS DocumentCount
    FROM [cases].[WorkbookType] wt
    LEFT JOIN [cases].[Workbook] w
        ON w.WorkbookTypeId = wt.WorkbookTypeId
       AND w.CaseId = @CaseId
       AND w.IsActive = 1
    OUTER APPLY
    (
        SELECT COUNT(*) AS DocumentCount
        FROM [cases].[WorkbookDocument] wd
        WHERE wd.WorkbookId = w.WorkbookId
          AND wd.IsActive = 1
    ) doc
    WHERE wt.IsActive = 1
    ORDER BY wt.DisplayOrder;
END;
