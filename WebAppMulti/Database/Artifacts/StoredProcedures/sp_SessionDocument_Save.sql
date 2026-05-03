CREATE PROCEDURE sp_SessionDocument_Save
(
    @SessionId INT,
    @FormTemplateId INT,
    @JsonData NVARCHAR(MAX)
)
AS
BEGIN
    INSERT INTO SessionDocuments (SessionId, FormTemplateId, JsonData)
    VALUES (@SessionId, @FormTemplateId, @JsonData);
END
