-- TABLE: dbo.ApplicationLogs
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[ApplicationLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MessageTemplate] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Level] [nvarchar](16) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TimeStamp] [datetime] NULL,
	[Exception] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Properties] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

