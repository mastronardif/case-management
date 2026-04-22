IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetAllPersons]') AND type = N'P')
    DROP PROCEDURE [dbo].[GetAllPersons]
GO

-- drop PROCEDURE GetAllPersons
CREATE PROCEDURE GetAllPersons
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [EmployeeKey], [FirstName] as EmployeeName FROM  [dbo].[DimEmployee];
END
-- use [AdventureWorksDW] exec GetAllPersons
GO
