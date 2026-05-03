-- TABLE: dbo.DimOrganization
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DimOrganization](
	[OrganizationKey] [int] IDENTITY(1,1) NOT NULL,
	[ParentOrganizationKey] [int] NULL,
	[PercentageOfOwnership] [nvarchar](16) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OrganizationName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CurrencyKey] [int] NULL,
 CONSTRAINT [PK_DimOrganization] PRIMARY KEY CLUSTERED 
(
	[OrganizationKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[DimOrganization]  WITH CHECK ADD  CONSTRAINT [FK_DimOrganization_DimCurrency] FOREIGN KEY([CurrencyKey])
REFERENCES [dbo].[DimCurrency] ([CurrencyKey])
ALTER TABLE [dbo].[DimOrganization] CHECK CONSTRAINT [FK_DimOrganization_DimCurrency]
ALTER TABLE [dbo].[DimOrganization]  WITH CHECK ADD  CONSTRAINT [FK_DimOrganization_DimOrganization] FOREIGN KEY([ParentOrganizationKey])
REFERENCES [dbo].[DimOrganization] ([OrganizationKey])
ALTER TABLE [dbo].[DimOrganization] CHECK CONSTRAINT [FK_DimOrganization_DimOrganization]
