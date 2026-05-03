IF OBJECT_ID(N'[dbo].[GetAllPersons]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GetAllPersons];
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