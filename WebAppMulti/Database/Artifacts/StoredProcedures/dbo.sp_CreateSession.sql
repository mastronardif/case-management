IF OBJECT_ID(N'[dbo].[sp_CreateSession]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sp_CreateSession];
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE sp_CreateSession
    @CaseId INT,
    @SessionDate DATETIME,
    @Notes NVARCHAR(MAX),
    @NewSessionId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Sessions (CaseId, SessionDate, Notes)
    VALUES (@CaseId, @SessionDate, @Notes);

    SET @NewSessionId = SCOPE_IDENTITY();
END

GO