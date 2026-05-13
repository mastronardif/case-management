create PROCEDURE [cases].[usp_Case_GetDocuments]
(
    @CaseId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DocumentId,
        d.CaseId,
        d.WorkbookQId,
        d.SessionId,
        d.DocumentType,
        d.Title,
        d.FileName,
        d.ContentType,
        d.CreatedDate,
        d.CreatedBy
    FROM cases.Document d
    WHERE d.CaseId = @CaseId
      AND d.IsActive = 1
    ORDER BY d.CreatedDate DESC;
END