-- TABLE: dbo.DimGeography
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DimGeography](
	[GeographyKey] [int] IDENTITY(1,1) NOT NULL,
	[City] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[StateProvinceCode] [nvarchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[StateProvinceName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CountryRegionCode] [nvarchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EnglishCountryRegionName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SpanishCountryRegionName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchCountryRegionName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PostalCode] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SalesTerritoryKey] [int] NULL,
	[IpAddressLocator] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_DimGeography_GeographyKey] PRIMARY KEY CLUSTERED 
(
	[GeographyKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[DimGeography]  WITH CHECK ADD  CONSTRAINT [FK_DimGeography_DimSalesTerritory] FOREIGN KEY([SalesTerritoryKey])
REFERENCES [dbo].[DimSalesTerritory] ([SalesTerritoryKey])
ALTER TABLE [dbo].[DimGeography] CHECK CONSTRAINT [FK_DimGeography_DimSalesTerritory]
