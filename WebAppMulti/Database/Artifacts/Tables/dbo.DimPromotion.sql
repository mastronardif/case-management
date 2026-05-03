-- TABLE: dbo.DimPromotion
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DimPromotion](
	[PromotionKey] [int] IDENTITY(1,1) NOT NULL,
	[PromotionAlternateKey] [int] NULL,
	[EnglishPromotionName] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SpanishPromotionName] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchPromotionName] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[DiscountPct] [float] NULL,
	[EnglishPromotionType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SpanishPromotionType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchPromotionType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EnglishPromotionCategory] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[SpanishPromotionCategory] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FrenchPromotionCategory] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NULL,
	[MinQty] [int] NULL,
	[MaxQty] [int] NULL,
 CONSTRAINT [PK_DimPromotion_PromotionKey] PRIMARY KEY CLUSTERED 
(
	[PromotionKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [AK_DimPromotion_PromotionAlternateKey] UNIQUE NONCLUSTERED 
(
	[PromotionAlternateKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

