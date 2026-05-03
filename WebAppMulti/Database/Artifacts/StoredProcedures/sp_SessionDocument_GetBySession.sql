CREATE PROCEDURE sp_SessionDocument_GetBySession
(
    @SessionId INT
)
AS
BEGIN
    SELECT 
        DocumentId,
        SessionId,
        FormTemplateId,
        JsonData
    FROM SessionDocuments
    WHERE SessionId = @SessionId;
END
