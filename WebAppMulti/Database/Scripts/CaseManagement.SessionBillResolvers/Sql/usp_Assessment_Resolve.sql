-- Template: copy and rename for each doc type (usp_Authorization_Resolve, etc.)
-- Called by DocResolveStep via ResolveDocAsync.
-- Signature: @DocId (JSON doc), @CaseId, @SessionId, @SrcDocId (original source doc)

-- exec [cases].[usp_Assessment_Resolve] @DocId = 224, @CaseId = 7, @SessionId = NULL, @SrcDocId = 164

CREATE OR ALTER PROCEDURE [cases].[usp_Assessment_Resolve]
(
    @DocId     INT,
    @CaseId    INT,
    @SessionId INT = NULL,
    @SrcDocId  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [cases].[Document] WHERE DocumentId = @DocId AND IsActive = 1)
        THROW 50001, 'JSON document not found or inactive.', 1;

    IF EXISTS (SELECT 1 FROM [cases].[Assessment] WHERE CaseId = @CaseId)
    BEGIN
        UPDATE [cases].[Assessment]
        SET JsonDocumentId    = @DocId,
            SourceDocumentId  = @SrcDocId
        WHERE CaseId = @CaseId;
    END
    ELSE
    BEGIN
        INSERT INTO [cases].[Assessment] (CaseId, JsonDocumentId, SourceDocumentId)
        VALUES (@CaseId, @DocId, @SrcDocId);
    END
END
GO
