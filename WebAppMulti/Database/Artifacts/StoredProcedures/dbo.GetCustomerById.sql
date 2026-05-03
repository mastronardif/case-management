IF OBJECT_ID(N'[dbo].[GetCustomerById]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GetCustomerById];
GO


CREATE   PROCEDURE [dbo].[GetCustomerById]
    @ID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        [CustomerKey],
        [FirstName],
        [LastName],
        [EmailAddress],
        [GeographyKey]
    FROM [dbo].[DimCustomer]
    WHERE [CustomerKey] = @ID;
END;

GO