CREATE OR ALTER PROCEDURE [cases].[usp_Pipeline_GetAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PipelineId,
        Name,
        Description,
        TemplateJson,
        ParamsSchema
    FROM [cases].[Pipeline]
    WHERE IsActive = 1
    ORDER BY Name;
END
GO
