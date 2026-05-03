-- TABLE: dbo.FactSurveyResponse
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[FactSurveyResponse](
	[SurveyResponseKey] [int] IDENTITY(1,1) NOT NULL,
	[DateKey] [int] NOT NULL,
	[CustomerKey] [int] NOT NULL,
	[ProductCategoryKey] [int] NOT NULL,
	[EnglishProductCategoryName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ProductSubcategoryKey] [int] NOT NULL,
	[EnglishProductSubcategoryName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Date] [datetime] NULL,
 CONSTRAINT [PK_FactSurveyResponse_SurveyResponseKey] PRIMARY KEY CLUSTERED 
(
	[SurveyResponseKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[FactSurveyResponse]  WITH CHECK ADD  CONSTRAINT [FK_FactSurveyResponse_CustomerKey] FOREIGN KEY([CustomerKey])
REFERENCES [dbo].[DimCustomer] ([CustomerKey])
ALTER TABLE [dbo].[FactSurveyResponse] CHECK CONSTRAINT [FK_FactSurveyResponse_CustomerKey]
ALTER TABLE [dbo].[FactSurveyResponse]  WITH CHECK ADD  CONSTRAINT [FK_FactSurveyResponse_DateKey] FOREIGN KEY([DateKey])
REFERENCES [dbo].[DimDate] ([DateKey])
ALTER TABLE [dbo].[FactSurveyResponse] CHECK CONSTRAINT [FK_FactSurveyResponse_DateKey]
