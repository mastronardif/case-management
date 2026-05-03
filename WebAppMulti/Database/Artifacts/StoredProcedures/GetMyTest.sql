
-- drop PROCEDURE GetMyTest
-- EXEC [dbo].[GetMyTest];         -- uses default 3
-- EXEC [dbo].[GetMyTest] @TopN=5; -- top 5 per country
CREATE   PROCEDURE [dbo].[GetMyTest]
 @TopN INT = 3  -- default value if not provided
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopN)
        g.EnglishCountryRegionName AS Country,
        g.City,
        COUNT(c.CustomerKey) AS TotalCustomers
    FROM DimGeography g
    JOIN DimCustomer c 
        ON g.GeographyKey = c.GeographyKey
    GROUP BY g.EnglishCountryRegionName, g.City
    ORDER BY COUNT(c.CustomerKey) DESC;

	SELECT 'Result set two' AS RS2

	SELECT 'Result set three' AS RS3
END-- use [AdventureWorksDW] exec GetAllCountryCityTotalCustomers
