CREATE PROCEDURE [cases].[usp_GetCalendar]
--    @ClientId INT = NULL
	@year INT  = NULL,
	@month INT = NULL
	--EXEC cases.usp_GetCalendar  @month = 1001 ,@year = 2026
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
    WHERE @year IS NULL OR ClientId = @year
    ORDER BY OpenedDate DESC;
END
