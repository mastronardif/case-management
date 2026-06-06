CREATE TABLE [cases].[Pipeline]
(
    [PipelineId]   INT            IDENTITY(1,1) NOT NULL,
    [Name]         VARCHAR(100)   NOT NULL,
    [Description]  NVARCHAR(500)  NULL,
    [TemplateJson] NVARCHAR(MAX)  NOT NULL,
    [ParamsSchema] NVARCHAR(MAX)  NOT NULL,
    [IsActive]     BIT            NOT NULL CONSTRAINT DF_Pipeline_IsActive DEFAULT (1),
    [CreatedDate]  DATETIME2(7)   NOT NULL CONSTRAINT DF_Pipeline_CreatedDate DEFAULT SYSUTCDATETIME(),
    [CreatedBy]    NVARCHAR(100)  NULL,
    CONSTRAINT PK_Pipeline PRIMARY KEY CLUSTERED ([PipelineId] ASC)
);
GO
