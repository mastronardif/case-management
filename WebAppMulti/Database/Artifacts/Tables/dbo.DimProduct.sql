-- TABLE: dbo.DimProduct
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DimProduct](
	[ProductKey] [int] IDENTITY(1,1) NOT NULL,
	[ProductAlternateKey] [nvarchar](25) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ProductSubcategoryKey] [int] NULL,
	[WeightUnitMeasureCode] [nchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SizeUnitMeasureCode] [nchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EnglishProductName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SpanishProductName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[FrenchProductName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[StandardCost] [money] NULL,
	[FinishedGoodsFlag] [bit] NOT NULL,
	[Color] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SafetyStockLevel] [smallint] NULL,
	[ReorderPoint] [smallint] NULL,
	[ListPrice] [money] NULL,
	[Size] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SizeRange] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Weight] [float] NULL,
	[DaysToManufacture] [int] NULL,
	[ProductLine] [nchar](2) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DealerPrice] [money] NULL,
	[Class] [nchar](2) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Style] [nchar](2) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ModelName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LargePhoto] [varbinary](max) NULL,
	[EnglishDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ChineseDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ArabicDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[HebrewDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[ThaiDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[GermanDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[JapaneseDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TurkishDescription] [nvarchar](400) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[Status] [nvarchar](7) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
 CONSTRAINT [PK_DimProduct_ProductKey] PRIMARY KEY CLUSTERED 
(
	[ProductKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [AK_DimProduct_ProductAlternateKey_StartDate] UNIQUE NONCLUSTERED 
(
	[ProductAlternateKey] ASC,
	[StartDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

ALTER TABLE [dbo].[DimProduct]  WITH CHECK ADD  CONSTRAINT [FK_DimProduct_DimProductSubcategory] FOREIGN KEY([ProductSubcategoryKey])
REFERENCES [dbo].[DimProductSubcategory] ([ProductSubcategoryKey])
ALTER TABLE [dbo].[DimProduct] CHECK CONSTRAINT [FK_DimProduct_DimProductSubcategory]
