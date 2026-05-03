CREATE PROCEDURE sp_InsertFormTemplate
    @Name NVARCHAR(200),
    @Description NVARCHAR(MAX),
    @JsonSchema NVARCHAR(MAX),
    @HtmlTemplate NVARCHAR(MAX),
    @NewTemplateId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO FormTemplates (Name, Description, JsonSchema, HtmlTemplate)
    VALUES (@Name, @Description, @JsonSchema, @HtmlTemplate);

    SET @NewTemplateId = SCOPE_IDENTITY();
END
