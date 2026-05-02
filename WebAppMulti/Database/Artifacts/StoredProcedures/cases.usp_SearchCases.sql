IF OBJECT_ID(N'[cases].[usp_SearchCases]', N'P') IS NOT NULL
    DROP PROCEDURE [cases].[usp_SearchCases];
GO

CREATE PROCEDURE [cases].[usp_SearchCases]
    @ClientId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CaseId,
        CaseNumber,
        Title,
        Status,
        Priority,
        ClientId,
        OpenedDate
    FROM cases.[Case]
    WHERE @ClientId IS NULL OR ClientId = @ClientId
    ORDER BY OpenedDate DESC;
END

GO