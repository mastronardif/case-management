-- TABLE: cases.Document
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [cases].[Document](
	[DocumentId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [uniqueidentifier] NOT NULL,
	[CaseId] [int] NOT NULL,
	[SessionId] [int] NULL,
	[WorkbookQId] [int] NULL,
	[DocumentType] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Title] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FileName] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ContentType] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FileData] [varbinary](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [cases].[Document] ADD  DEFAULT (newid()) FOR [VersionId]
ALTER TABLE [cases].[Document] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
ALTER TABLE [cases].[Document] ADD  DEFAULT ((1)) FOR [IsActive]
