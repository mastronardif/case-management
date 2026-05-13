-- TABLE: cases.Session
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [cases].[Session](
	[SessionId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[SessionDate] [datetime2](7) NOT NULL,
	[DurationMinutes] [int] NULL,
	[ProviderId] [int] NULL,
	[Location] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NotesSummary] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[SessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [cases].[Session] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
