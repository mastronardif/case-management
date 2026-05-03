
CREATE   PROCEDURE [dbo].[UpsertDepartmentGroup]
    @DepartmentGroupKey INT = NULL,               -- optional for updates
    @DepartmentGroupName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- If a DepartmentGroupKey is provided and exists, update it
    IF EXISTS (SELECT 1 FROM [dbo].[DimDepartmentGroup] WHERE DepartmentGroupKey = @DepartmentGroupKey)
    BEGIN
        UPDATE [dbo].[DimDepartmentGroup]
        SET DepartmentGroupName = @DepartmentGroupName
        WHERE DepartmentGroupKey = @DepartmentGroupKey;

        SELECT 
            @DepartmentGroupKey AS DepartmentGroupKey,
            'Updated' AS ActionTaken;
    END
    ELSE
    BEGIN
        -- Insert new record
        INSERT INTO [dbo].[DimDepartmentGroup] (DepartmentGroupName)
        VALUES (@DepartmentGroupName);

        DECLARE @NewId INT = SCOPE_IDENTITY();

        SELECT 
            @NewId AS DepartmentGroupKey,
            'Inserted' AS ActionTaken;
    END
END
