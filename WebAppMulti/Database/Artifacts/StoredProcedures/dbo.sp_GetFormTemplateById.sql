IF OBJECT_ID(N'[dbo].[sp_GetFormTemplateById]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetFormTemplateById];
GO

CREATE PROCEDURE sp_GetFormTemplateById
    @TemplateId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT HtmlTemplate
    FROM FormTemplates
    WHERE FormTemplateId = @TemplateId;
END
GO