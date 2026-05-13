-- TABLE: cases.WorkbookDocument
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [cases].[WorkbookDocument](
	[WorkbookDocumentId] [int] IDENTITY(1,1) NOT NULL,
	[WorkbookId] [int] NOT NULL,
	[DocumentId] [int] NOT NULL,
	[DocumentType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedBy] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_WorkbookDocument] PRIMARY KEY CLUSTERED 
(
	[WorkbookDocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [cases].[WorkbookDocument] ADD  CONSTRAINT [DF_WorkbookDocument_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
ALTER TABLE [cases].[WorkbookDocument] ADD  CONSTRAINT [DF_WorkbookDocument_IsActive]  DEFAULT ((1)) FOR [IsActive]
ALTER TABLE [cases].[WorkbookDocument]  WITH CHECK ADD  CONSTRAINT [FK_WorkbookDocument_Workbook] FOREIGN KEY([WorkbookId])
REFERENCES [cases].[Workbook] ([WorkbookId])
ALTER TABLE [cases].[WorkbookDocument] CHECK CONSTRAINT [FK_WorkbookDocument_Workbook]
