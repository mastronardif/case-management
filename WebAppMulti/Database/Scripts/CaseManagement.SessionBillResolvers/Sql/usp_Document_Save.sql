CREATE OR ALTER PROCEDURE [cases].[usp_Document_Save]
    @CaseId        INT             = NULL,
    @CaseNumber    NVARCHAR(50)    = NULL,
    @SessionId     INT             = NULL,
    @WorkbookQId   INT             = NULL,
    @DocumentType  VARCHAR(50),
    @Title         NVARCHAR(200)   = NULL,
    @FileName      NVARCHAR(255)   = NULL,
    @ContentType   VARCHAR(100),
    @FileData      VARBINARY(MAX),
    @CreatedBy     NVARCHAR(100)   = NULL,
    @DocumentId    INT             = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @DocumentId IS NOT NULL
       AND EXISTS (SELECT 1 FROM [cases].[Document] WHERE DocumentId = @DocumentId AND IsActive = 1)
    BEGIN
        UPDATE [cases].[Document]
        SET
            VersionId    = NEWID(),
            DocumentType = @DocumentType,
            Title        = @Title,
            FileName     = @FileName,
            ContentType  = @ContentType,
            FileData     = @FileData,
            CreatedDate  = SYSUTCDATETIME(),
            CreatedBy    = @CreatedBy
        WHERE DocumentId = @DocumentId;
        -- @DocumentId is already set; OUTPUT returns it unchanged
    END
    ELSE
    BEGIN
        INSERT INTO [cases].[Document]
        (
            VersionId, CaseId, CaseNumber, SessionId, WorkbookQId,
            DocumentType, Title, FileName, ContentType, FileData,
            CreatedDate, CreatedBy, IsActive
        )
        VALUES
        (
            NEWID(), @CaseId, @CaseNumber, @SessionId, @WorkbookQId,
            @DocumentType, @Title, @FileName, @ContentType, @FileData,
            SYSUTCDATETIME(), @CreatedBy, 1
        );

        SET @DocumentId = SCOPE_IDENTITY();
    END
END
GO
