-- TABLE: dbo.ProspectiveBuyer
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
CREATE TABLE [dbo].[ProspectiveBuyer](
	[ProspectiveBuyerKey] [int] IDENTITY(1,1) NOT NULL,
	[ProspectAlternateKey] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[FirstName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[MiddleName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[LastName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[BirthDate] [datetime] NULL,
	[MaritalStatus] [nchar](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Gender] [nvarchar](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[EmailAddress] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[YearlyIncome] [money] NULL,
	[TotalChildren] [tinyint] NULL,
	[NumberChildrenAtHome] [tinyint] NULL,
	[Education] [nvarchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Occupation] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[HouseOwnerFlag] [nchar](1) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[NumberCarsOwned] [tinyint] NULL,
	[AddressLine1] [nvarchar](120) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[AddressLine2] [nvarchar](120) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[City] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[StateProvinceCode] [nvarchar](3) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PostalCode] [nvarchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Phone] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Salutation] [nvarchar](8) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Unknown] [int] NULL,
 CONSTRAINT [PK_ProspectiveBuyer_ProspectiveBuyerKey] PRIMARY KEY CLUSTERED 
(
	[ProspectiveBuyerKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

