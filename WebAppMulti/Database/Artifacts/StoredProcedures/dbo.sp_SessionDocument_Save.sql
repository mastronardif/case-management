IF OBJECT_ID(N'[dbo].[sp_SessionDocument_Save]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_SessionDocument_Save];
GO

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

GO