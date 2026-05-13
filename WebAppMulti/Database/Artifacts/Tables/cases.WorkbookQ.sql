-- TABLE: cases.WorkbookQ
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [cases].[WorkbookQ](
	[WorkbookQId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Description] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[QueryKey] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkbookQId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [cases].[WorkbookQ] ADD  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [cases].[WorkbookQ] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
