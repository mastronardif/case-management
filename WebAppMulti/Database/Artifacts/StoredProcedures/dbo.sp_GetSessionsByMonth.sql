IF OBJECT_ID(N'[dbo].[sp_GetSessionsByMonth]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_GetSessionsByMonth];
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE sp_GetSessionsByMonth
    @CaseId INT,
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.SessionId,
        s.CaseId,
        s.SessionDate,
        s.Notes
    FROM Sessions s
    WHERE s.CaseId = @CaseId
      AND YEAR(s.SessionDate) = @Year
      AND MONTH(s.SessionDate) = @Month
    ORDER BY s.SessionDate;
END



GO