
CREATE     PROCEDURE [cases].[usp_WorkbookQ_GetDocuments]
(
    @CaseId INT,
    @WorkbookQId INT
)
AS
/*
EXEC cases.usp_WorkbookQ_GetDocuments @CaseId = 1, @WorkbookQId = 1001
*/
BEGIN
    SET NOCOUNT ON;

SELECT
    r.DocumentType,
    d.DocumentId,
    d.Title,
    d.ContentType
FROM cases.WorkbookQRule r
LEFT JOIN cases.Document d
    ON d.WorkbookQId = r.WorkbookQId
    AND d.DocumentType = r.DocumentType
    AND d.CaseId = @CaseId
    AND d.IsActive = 1
WHERE r.WorkbookQId = @WorkbookQId
    ORDER BY d.CreatedDate DESC;
END
