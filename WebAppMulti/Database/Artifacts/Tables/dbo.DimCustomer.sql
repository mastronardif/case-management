-- TABLE: dbo.DimCustomer
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DimCustomer](
	[CustomerKey] [int] IDENTITY(1,1) NOT NULL,
	[GeographyKey] [int] NULL,
	[CustomerAlternateKey] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Title] [nvarchar](8) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FirstName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MiddleName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LastName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NameStyle] [bit] NULL,
	[BirthDate] [date] NULL,
	[MaritalStatus] [nchar](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Suffix] [nvarchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Gender] [nvarchar](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EmailAddress] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[YearlyIncome] [money] NULL,
	[TotalChildren] [tinyint] NULL,
	[NumberChildrenAtHome] [tinyint] NULL,
	[EnglishEducation] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SpanishEducation] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchEducation] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EnglishOccupation] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SpanishOccupation] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchOccupation] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[HouseOwnerFlag] [nchar](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NumberCarsOwned] [tinyint] NULL,
	[AddressLine1] [nvarchar](120) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AddressLine2] [nvarchar](120) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Phone] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DateFirstPurchase] [date] NULL,
	[CommuteDistance] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_DimCustomer_CustomerKey] PRIMARY KEY CLUSTERED 
(
	[CustomerKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

SET ANSI_PADDING ON

CREATE UNIQUE NONCLUSTERED INDEX [IX_DimCustomer_CustomerAlternateKey] ON [dbo].[DimCustomer]
(
	[CustomerAlternateKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
ALTER TABLE [dbo].[DimCustomer]  WITH CHECK ADD  CONSTRAINT [FK_DimCustomer_DimGeography] FOREIGN KEY([GeographyKey])
REFERENCES [dbo].[DimGeography] ([GeographyKey])
ALTER TABLE [dbo].[DimCustomer] CHECK CONSTRAINT [FK_DimCustomer_DimGeography]
