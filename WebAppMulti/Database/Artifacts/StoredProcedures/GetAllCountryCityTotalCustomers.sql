-- drop PROCEDURE GetAllCountryCityTotalCustomers
-- EXEC [dbo].[GetAllCountryCityTotalCustomers];         -- uses default 3
-- EXEC [dbo].[GetAllCountryCityTotalCustomers] @TopN=5; -- top 5 per country
CREATE PROCEDURE [dbo].[GetAllCountryCityTotalCustomers]
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
END-- use [AdventureWorksDW] exec GetAllCountryCityTotalCustomers