-- TABLE: cases.Workbook
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [cases].[Workbook](
	[WorkbookId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[WorkbookTypeId] [int] NOT NULL,
	[Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[DueDate] [datetime2](7) NULL,
	[CompletedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ModifiedBy] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkbookId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [cases].[Workbook] ADD  DEFAULT ('Pending') FOR [Status]
ALTER TABLE [cases].[Workbook] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
ALTER TABLE [cases].[Workbook] ADD  DEFAULT ((1)) FOR [IsActive]
