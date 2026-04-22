IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetCustomerById]') AND type = N'P')
    DROP PROCEDURE [dbo].[GetCustomerById]
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
