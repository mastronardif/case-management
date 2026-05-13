-- TABLE: cases.WorkbookType
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [cases].[WorkbookType](
	[WorkbookTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_WorkbookType] PRIMARY KEY CLUSTERED 
(
	[WorkbookTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_WorkbookType_Name] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [cases].[WorkbookType] ADD  CONSTRAINT [DF_WorkbookType_IsRequired]  DEFAULT ((1)) FOR [IsRequired]
ALTER TABLE [cases].[WorkbookType] ADD  CONSTRAINT [DF_WorkbookType_IsActive]  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [cases].[WorkbookType] ADD  CONSTRAINT [DF_WorkbookType_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
