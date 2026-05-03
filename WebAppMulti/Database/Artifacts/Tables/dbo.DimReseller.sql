-- TABLE: dbo.DimReseller
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[DimReseller](
	[ResellerKey] [int] IDENTITY(1,1) NOT NULL,
	[GeographyKey] [int] NULL,
	[ResellerAlternateKey] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Phone] [nvarchar](25) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BusinessType] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[ResellerName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[NumberEmployees] [int] NULL,
	[OrderFrequency] [char](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[OrderMonth] [tinyint] NULL,
	[FirstOrderYear] [int] NULL,
	[LastOrderYear] [int] NULL,
	[ProductLine] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AddressLine1] [nvarchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AddressLine2] [nvarchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AnnualSales] [money] NULL,
	[BankName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MinPaymentType] [tinyint] NULL,
	[MinPaymentAmount] [money] NULL,
	[AnnualRevenue] [money] NULL,
	[YearOpened] [int] NULL,
 CONSTRAINT [PK_DimReseller_ResellerKey] PRIMARY KEY CLUSTERED 
(
	[ResellerKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [AK_DimReseller_ResellerAlternateKey] UNIQUE NONCLUSTERED 
(
	[ResellerAlternateKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

ALTER TABLE [dbo].[DimReseller]  WITH CHECK ADD  CONSTRAINT [FK_DimReseller_DimGeography] FOREIGN KEY([GeographyKey])
REFERENCES [dbo].[DimGeography] ([GeographyKey])
ALTER TABLE [dbo].[DimReseller] CHECK CONSTRAINT [FK_DimReseller_DimGeography]
