CREATE TABLE [dbo].[sysdiagrams] (
    [name] sysname NOT NULL,    [principal_id] int NOT NULL,    [diagram_id] int IDENTITY(1,1) NOT NULL,    [version] int NULL,    [definition] varbinary(MAX) NULL
,
    CONSTRAINT [PK__sysdiagr__C2B05B616B24EA82] PRIMARY KEY CLUSTERED ([diagram_id])
);
GO
