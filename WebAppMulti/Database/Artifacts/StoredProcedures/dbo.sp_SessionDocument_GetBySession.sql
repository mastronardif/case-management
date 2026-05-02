IF OBJECT_ID(N'[dbo].[sp_SessionDocument_GetBySession]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_SessionDocument_GetBySession];
GO

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

GO