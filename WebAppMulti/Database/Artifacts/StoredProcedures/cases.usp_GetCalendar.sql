IF OBJECT_ID(N'[cases].[usp_GetCalendar]', N'P') IS NOT NULL
    DROP PROCEDURE [cases].[usp_GetCalendar];
GO

CREATE PROCEDURE [cases].[usp_GetCalendar]
--    @ClientId INT = NULL
	@year INT  = NULL,
	@month INT = NULL
	--EXEC calendar.usp_GetCalendar @year = 2026, @month = 4
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

GO