-- exec [cases].[usp_SearchCases] =1005
CREATE PROCEDURE [cases].[usp_SearchCases]
    @ClientId INT = NULL
    --EXEC cases.usp_SearchCases  @ClientId = 1005
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
