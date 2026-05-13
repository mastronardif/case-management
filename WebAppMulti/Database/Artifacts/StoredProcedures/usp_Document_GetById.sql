CREATE   PROCEDURE [cases].[usp_Document_GetById]
(
    @DocumentId INT
)
AS
/*
EXEC cases.[usp_Document_GetById] @DocumentId = 1
*/
BEGIN
    SET NOCOUNT ON;

    SELECT
        DocumentId,
        VersionId,
        CaseId,
        WorkbookQId,
        SessionId,
        DocumentType,
        Title,
        FileName,
        ContentType,
        FileData,
        CreatedDate,
        CreatedBy,
        IsActive
    FROM [cases].[Document]
    WHERE
        DocumentId = @DocumentId
        AND IsActive = 1;
END