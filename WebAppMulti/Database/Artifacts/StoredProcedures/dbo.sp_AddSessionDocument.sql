IF OBJECT_ID(N'[dbo].[sp_AddSessionDocument]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_AddSessionDocument];
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE sp_AddSessionDocument
    @SessionId INT,
    @FormTemplateId INT,
    @JsonData NVARCHAR(MAX),
    @NewDocumentId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SessionDocuments (SessionId, FormTemplateId, JsonData)
    VALUES (@SessionId, @FormTemplateId, @JsonData);

    SET @NewDocumentId = SCOPE_IDENTITY();
END


GO