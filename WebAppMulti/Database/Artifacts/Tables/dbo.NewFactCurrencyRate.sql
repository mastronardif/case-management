-- TABLE: dbo.NewFactCurrencyRate
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[NewFactCurrencyRate](
	[AverageRate] [real] NULL,
	[CurrencyID] [nvarchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CurrencyDate] [date] NULL,
	[EndOfDayRate] [real] NULL,
	[CurrencyKey] [int] NULL,
	[DateKey] [int] NULL
) ON [PRIMARY]

