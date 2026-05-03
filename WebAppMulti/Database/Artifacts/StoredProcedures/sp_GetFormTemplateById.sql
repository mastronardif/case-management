CREATE PROCEDURE sp_GetFormTemplateById
    @TemplateId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT HtmlTemplate
    FROM FormTemplates
    WHERE FormTemplateId = @TemplateId;
END