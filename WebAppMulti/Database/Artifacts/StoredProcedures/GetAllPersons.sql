-- drop PROCEDURE GetAllPersons
CREATE PROCEDURE GetAllPersons
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [EmployeeKey], [FirstName] as EmployeeName FROM  [dbo].[DimEmployee];
END
-- use [AdventureWorksDW] exec GetAllPersons