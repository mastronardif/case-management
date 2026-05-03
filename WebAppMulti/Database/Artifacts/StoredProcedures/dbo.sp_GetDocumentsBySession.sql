IF OBJECT_ID(N'[dbo].[sp_GetDocumentsBySession]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetDocumentsBySession];
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE sp_GetDocumentsBySession
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DocumentId,
        SessionId,
        FormTemplateId,
        JsonData
    FROM SessionDocuments
    WHERE SessionId = @SessionId
    ORDER BY DocumentId;
END




GO