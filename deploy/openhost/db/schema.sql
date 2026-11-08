-- CaseManagement schema only (no data). Generated 2026-11-05 12:39
SET NOCOUNT ON;
GO
CREATE SCHEMA [cases];
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Session](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [int] NULL,
	[CaseId] [int] NOT NULL,
	[SessionDate] [date] NULL,
	[ProviderId] [int] NULL,
	[Location] [nvarchar](200) NULL,
	[NotesSummary] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[StartTime] [time](0) NULL,
	[EndTime] [time](0) NULL,
	[CPTCode] [nvarchar](20) NULL,
	[CPTModifier1] [nvarchar](10) NULL,
	[CPTModifier2] [nvarchar](10) NULL,
	[Units] [decimal](10, 2) NULL,
	[DurationMinutes] [int] NULL,
	[BillingStatus] [nvarchar](50) NULL,
	[SessionStatus] [nvarchar](50) NULL,
	[ApprovedBy] [nvarchar](100) NULL,
	[ApprovedDate] [datetime2](0) NULL,
	[InvoiceId] [int] NULL,
	[AuthorizationNumber] [nvarchar](100) NULL,
	[PlaceOfServiceCode] [nvarchar](10) NULL,
	[SourceDocumentId] [int] NULL,
	[ReviewedBy] [nvarchar](100) NULL,
	[ReviewedDate] [datetime2](0) NULL,
	[JsonDocumentId] [int] NULL,
	[ClientName] [nvarchar](100) NULL,
 CONSTRAINT [PK_Session] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[ClaimSession](
	[ClaimSessionId] [int] IDENTITY(1,1) NOT NULL,
	[ClaimId] [int] NOT NULL,
	[SessionDocumentId] [int] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[LineNumber] [int] NULL,
	[ChargeAmount] [decimal](10, 2) NULL,
	[Units] [decimal](6, 2) NULL,
	[SessionStatus] [varchar](25) NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ClaimSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_ClaimSession] UNIQUE NONCLUSTERED 
(
	[ClaimId] ASC,
	[SessionDocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[queueClaimsToBeCreated](
	[QueueClaimId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[Status] [varchar](50) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[SpecDocumentId] [int] NULL,
 CONSTRAINT [PK_queueClaimsToBeCreated] PRIMARY KEY CLUSTERED 
(
	[QueueClaimId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[QueueClaimSession](
	[QueueClaimSessionId] [int] IDENTITY(1,1) NOT NULL,
	[QueueClaimId] [int] NOT NULL,
	[SessionDocumentId] [int] NOT NULL,
 CONSTRAINT [PK_QueueClaimSession] PRIMARY KEY CLUSTERED 
(
	[QueueClaimSessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Claim](
	[ClaimId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[ClaimNumber] [varchar](50) NULL,
	[Status] [varchar](50) NULL,
	[GeneratedDate] [datetime2](7) NULL,
	[EdiDocumentId] [int] NULL,
	[IsActive] [bit] NOT NULL,
	[IsVoided] [bit] NOT NULL,
	[VoidReason] [nvarchar](250) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[SubmittedDate] [datetime2](7) NULL,
	[AcknowledgedDate] [datetime2](7) NULL,
	[PaidDate] [datetime2](7) NULL,
	[TotalCharge] [decimal](10, 2) NULL,
	[EdiControlNumber] [varchar](20) NULL,
	[PayerClaimNumber] [varchar](50) NULL,
	[VoidDate] [datetime2](7) NULL,
	[SourcesDocumentId] [int] NULL,
	[AvailityReviewDocumentId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[ClaimId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [cases].[vw_SessionClaimStatus]
-- select * from [cases].[vw_SessionClaimStatus]
AS
SELECT
    ss.CaseId,
    ss.SourceDocumentId, ss.JsonDocumentId,
    ss.SessionDate,
    ss.CPTCode,
    ss.CPTModifier1,
    DATEDIFF(MINUTE, ss.StartTime, ss.EndTime) AS DurationMinutes,
    CEILING(DATEDIFF(MINUTE, ss.StartTime, ss.EndTime) / 15.0) AS Duration15MinUnits,
    cs.ClaimId, c.ClaimNumber, c.Status AS ClaimStatus,
    q.QueueClaimId,

    CASE
        WHEN cs.ClaimSessionId IS NOT NULL AND c.IsVoided = 1
            THEN 'Claim Voided'
        WHEN cs.ClaimSessionId IS NOT NULL AND c.IsActive = 0
            THEN 'Claim Inactive'
        WHEN cs.ClaimSessionId IS NOT NULL AND c.Status = 'Pending'
            THEN 'Claim Pending'
        WHEN cs.ClaimSessionId IS NOT NULL AND c.Status = 'Submitted'
            THEN 'Claim Submitted'
        WHEN cs.ClaimSessionId IS NOT NULL AND c.Status = 'Accepted'
            THEN 'Claim Accepted'
        WHEN cs.ClaimSessionId IS NOT NULL AND c.Status = 'Rejected'
            THEN 'Claim Rejected'
        WHEN cs.ClaimSessionId IS NOT NULL AND c.Status = 'Paid'
            THEN 'Claim Paid'
        WHEN cs.ClaimSessionId IS NOT NULL
            THEN c.Status
        WHEN q.QueueClaimId IS NOT NULL
            THEN 'Queued'
        ELSE 'Not Claimed'
    END AS SessionClaimStatus

FROM cases.[Session] ss

LEFT JOIN cases.ClaimSession cs
    ON cs.SessionDocumentId = ss.JsonDocumentId

LEFT JOIN cases.Claim c
    ON c.ClaimId = cs.ClaimId

LEFT JOIN cases.QueueClaimSession qcs
    ON qcs.SessionDocumentId = ss.JsonDocumentId

LEFT JOIN cases.queueClaimsToBeCreated q
    ON q.QueueClaimId = qcs.QueueClaimId AND q.Status = 'Pending';

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Case](
	[CaseId] [int] IDENTITY(1,1) NOT NULL,
	[PublicId] [uniqueidentifier] NOT NULL,
	[CaseNumber] [nvarchar](50) NOT NULL,
	[Title] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Status] [nvarchar](50) NOT NULL,
	[Priority] [nvarchar](25) NULL,
	[ClientId] [int] NULL,
	[OpenedDate] [date] NOT NULL,
	[ClosedDate] [datetime2](0) NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[ModifiedDate] [datetime2](0) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Case] PRIMARY KEY CLUSTERED 
(
	[CaseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Case_CaseNumber] UNIQUE NONCLUSTERED 
(
	[CaseNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Case_PublicId] UNIQUE NONCLUSTERED 
(
	[PublicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Assessment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[AssessmentDate] [date] NULL,
	[AssessorName] [nvarchar](200) NULL,
	[DiagnosisCode] [varchar](20) NULL,
	[DocumentJson] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[SourceDocumentId] [int] NULL,
	[JsonDocumentId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW [cases].[vw_CaseSearch] AS
SELECT
    cc.CaseId                AS Id,
    cc.CaseNumber             AS Number,
    cc.Title                  AS Title,
    aa.[JsonDocumentId]       AS JId,
    aa.[SourceDocumentId]     AS Src,
    cc.IsActive                AS Active,
    cc.OpenedDate              AS Opened
FROM cases.[Case] cc
LEFT JOIN [cases].[Assessment] aa ON aa.CaseId = cc.CaseId;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Payer](
	[PayerId] [int] IDENTITY(1,1) NOT NULL,
	[PublicId] [uniqueidentifier] NOT NULL,
	[PayerCode] [nvarchar](50) NULL,
	[PayerName] [nvarchar](200) NOT NULL,
	[PayerType] [nvarchar](50) NULL,
	[StateCode] [nvarchar](10) NULL,
	[PhoneNumber] [nvarchar](50) NULL,
	[Website] [nvarchar](500) NULL,
	[PayerIdentifier] [nvarchar](100) NULL,
	[EDIReceiverId] [nvarchar](100) NULL,
	[ClaimsSubmissionMethod] [nvarchar](50) NULL,
	[Notes] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[ModifiedDate] [datetime2](0) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NOT NULL,
	[CodeQualifier] [nvarchar](10) NULL,
	[AvailitySubmitterId] [nvarchar](50) NULL,
 CONSTRAINT [PK_Payer] PRIMARY KEY CLUSTERED 
(
	[PayerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Payer_PayerName] UNIQUE NONCLUSTERED 
(
	[PayerName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Payer_PublicId] UNIQUE NONCLUSTERED 
(
	[PublicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[InsuranceCoverage](
	[InsuranceCoverageId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[PayerId] [int] NOT NULL,
	[MemberId] [varchar](100) NOT NULL,
	[GroupNumber] [varchar](100) NULL,
	[SubscriberName] [nvarchar](200) NULL,
	[RelationshipCode] [varchar](10) NULL,
	[EffectiveDate] [date] NULL,
	[TerminationDate] [date] NULL,
	[SubscriberFirstName] [nvarchar](100) NULL,
	[SubscriberLastName] [nvarchar](100) NULL,
	[SubscriberMiddleName] [nvarchar](100) NULL,
	[SubscriberDateOfBirth] [date] NULL,
	[SubscriberGender] [char](1) NULL,
	[SubscriberAddress1] [nvarchar](200) NULL,
	[SubscriberAddress2] [nvarchar](200) NULL,
	[SubscriberCity] [nvarchar](100) NULL,
	[SubscriberState] [char](2) NULL,
	[SubscriberZipCode] [varchar](10) NULL,
	[SubscriberPhone] [varchar](25) NULL,
	[SubscriberEmail] [nvarchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[InsuranceCoverageId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [cases].[vw_InsuranceCoverageAdmin] AS
SELECT
    ic.InsuranceCoverageId, c.CaseNumber, ic.CaseId, p.PayerName,
    ic.MemberId, ic.GroupNumber, ic.SubscriberName, ic.RelationshipCode,
    ic.EffectiveDate, ic.TerminationDate
FROM   cases.InsuranceCoverage ic
JOIN   cases.[Case] c ON c.CaseId = ic.CaseId
LEFT JOIN cases.Payer p ON p.PayerId = ic.PayerId;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Patient](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[MemberId] [nvarchar](50) NULL,
	[MedicaidId] [nvarchar](50) NULL,
	[Suffix] [nvarchar](20) NULL,
	[FirstName] [nvarchar](100) NOT NULL,
	[MiddleName] [nvarchar](100) NULL,
	[LastName] [nvarchar](100) NOT NULL,
	[DateOfBirth] [date] NULL,
	[Gender] [char](1) NULL,
	[Address1] [nvarchar](200) NULL,
	[Address2] [nvarchar](200) NULL,
	[City] [nvarchar](100) NULL,
	[State] [char](2) NULL,
	[ZipCode] [nvarchar](15) NULL,
	[Phone] [nvarchar](25) NULL,
	[Email] [nvarchar](200) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
 CONSTRAINT [PK_Patient] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Patient_CaseId] UNIQUE NONCLUSTERED 
(
	[CaseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [cases].[vw_PatientAdmin] AS
SELECT
    pa.Id, c.CaseNumber, pa.CaseId, pa.FirstName, pa.MiddleName, pa.LastName,
    pa.DateOfBirth, pa.Gender, pa.MemberId, pa.MedicaidId, pa.Phone, pa.Email, pa.IsActive
FROM   cases.Patient pa
JOIN   cases.[Case] c ON c.CaseId = pa.CaseId;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Authorization](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[AuthorizationNumber] [nvarchar](max) NULL,
	[StartDate] [date] NULL,
	[EndDate] [date] NULL,
	[AuthorizedUnits] [int] NULL,
	[Notes] [nvarchar](max) NULL,
	[SourceDocumentId] [int] NULL,
	[JsonDocumentId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [cases].[vw_AuthorizationAdmin] AS
SELECT
    a.Id, c.CaseNumber, a.CaseId, a.AuthorizationNumber, a.StartDate, a.EndDate,
    a.AuthorizedUnits, a.Notes, a.SourceDocumentId, a.JsonDocumentId
FROM   cases.[Authorization] a
JOIN   cases.[Case] c ON c.CaseId = a.CaseId;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE VIEW [cases].[vw_PayerAdmin] AS
SELECT
    PayerId, PayerCode, PayerName, PayerType, StateCode, PhoneNumber, Website,
    PayerIdentifier, EDIReceiverId, ClaimsSubmissionMethod, AvailitySubmitterId, IsActive, Notes
FROM cases.Payer;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[SessionSchedule](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[DayOfWeek] [varchar](3) NOT NULL,
	[StartTime] [time](7) NOT NULL,
	[EndTime] [time](7) NOT NULL,
	[ProviderId] [int] NULL,
	[CPTCode] [varchar](20) NULL,
	[CPTModifier1] [varchar](10) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
 CONSTRAINT [PK_SessionSchedule] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Provider](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProviderName] [nvarchar](200) NOT NULL,
	[NPI] [varchar](20) NOT NULL,
	[TaxId] [varchar](20) NOT NULL,
	[Address1] [nvarchar](200) NULL,
	[Address2] [nvarchar](200) NULL,
	[City] [nvarchar](100) NULL,
	[State] [char](2) NULL,
	[ZipCode] [varchar](10) NULL,
	[Phone] [varchar](20) NULL,
	[IsActive] [bit] NOT NULL,
	[ProviderCode] [nvarchar](50) NULL,
	[ProviderType] [nvarchar](50) NULL,
	[Email] [nvarchar](200) NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[ModifiedDate] [datetime2](0) NULL,
	[TaxonomyCode] [varchar](20) NULL,
	[FirstName] [nvarchar](100) NULL,
	[LastName] [nvarchar](100) NULL,
	[ClinicianUsername] [nvarchar](100) NULL,
	[ProviderRole] [varchar](25) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [cases].[vw_SessionScheduleAdmin] AS
SELECT
    ss.Id, c.CaseNumber, ss.CaseId, ss.DayOfWeek, ss.StartTime, ss.EndTime,
    COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(pr.FirstName, ' ', pr.LastName))), ''), pr.ProviderName) AS Provider,
    ss.CPTCode, ss.CPTModifier1, ss.IsActive
FROM   cases.SessionSchedule ss
JOIN   cases.[Case] c ON c.CaseId = ss.CaseId
LEFT JOIN cases.Provider pr ON pr.Id = ss.ProviderId;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[MyConstants](
	[MyConstantId] [int] IDENTITY(1,1) NOT NULL,
	[Key] [varchar](200) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	[Type] [varchar](50) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_MyConstants] PRIMARY KEY CLUSTERED 
(
	[MyConstantId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_MyConstants_Key] UNIQUE NONCLUSTERED 
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Logs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[MessageTemplate] [nvarchar](max) NULL,
	[Level] [nvarchar](max) NULL,
	[TimeStamp] [datetime] NULL,
	[Exception] [nvarchar](max) NULL,
	[Properties] [nvarchar](max) NULL,
 CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[ProjectorRule](
	[ProjectorRuleId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Version] [varchar](20) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[ProjectionDocumentId] [int] NOT NULL,
	[RuleDocumentId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
 CONSTRAINT [PK_ProjectorRule] PRIMARY KEY CLUSTERED 
(
	[ProjectorRuleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[PriorAuthForm](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NULL,
	[PatientName] [nvarchar](200) NULL,
	[PatientDOB] [date] NULL,
	[MemberId] [nvarchar](100) NULL,
	[InsuranceName] [nvarchar](200) NULL,
	[DiagnosisCode] [nvarchar](50) NULL,
	[ProviderName] [nvarchar](200) NULL,
	[ProviderNPI] [nvarchar](20) NULL,
	[AuthorizationNumber] [nvarchar](100) NULL,
	[DocumentJson] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[SourceDocumentId] [int] NULL,
	[JsonDocumentId] [int] NULL,
 CONSTRAINT [PK_PriorAuthForm] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[PracticeConfiguration](
	[PracticeConfigurationId] [int] IDENTITY(1,1) NOT NULL,
	[PracticeName] [varchar](100) NOT NULL,
	[BillingProviderName] [varchar](100) NOT NULL,
	[BillingProviderNPI] [varchar](10) NOT NULL,
	[BillingProviderTaxonomy] [varchar](20) NULL,
	[TaxId] [varchar](20) NOT NULL,
	[Address1] [varchar](100) NOT NULL,
	[Address2] [varchar](100) NULL,
	[City] [varchar](50) NOT NULL,
	[State] [char](2) NOT NULL,
	[Zip] [varchar](10) NOT NULL,
	[Phone] [varchar](20) NULL,
	[Email] [varchar](100) NULL,
	[SenderIdQualifier] [char](2) NOT NULL,
	[SenderId] [varchar](30) NOT NULL,
	[ReceiverIdQualifier] [char](2) NOT NULL,
	[ReceiverId] [varchar](30) NOT NULL,
	[SubmitterName] [varchar](100) NOT NULL,
	[SubmitterIdentifier] [varchar](80) NOT NULL,
	[FunctionalIdentifierCode] [varchar](2) NOT NULL,
	[VersionIdentifier] [varchar](20) NOT NULL,
	[TestIndicator] [char](1) NOT NULL,
	[NextIsaControlNumber] [int] NOT NULL,
	[NextGsControlNumber] [int] NOT NULL,
	[NextStControlNumber] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ModifiedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_PracticeConfiguration] PRIMARY KEY CLUSTERED 
(
	[PracticeConfigurationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Pipeline](
	[PipelineId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[DocumentId] [int] NOT NULL,
 CONSTRAINT [PK_Pipeline] PRIMARY KEY CLUSTERED 
(
	[PipelineId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[TableFieldMap](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TableName] [sysname] NOT NULL,
	[JsonDocId] [int] NOT NULL,
	[Version] [int] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ModifiedDate] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[ShortLink](
	[ShortLinkId] [int] IDENTITY(1,1) NOT NULL,
	[TargetUrl] [nvarchar](2000) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ExpiresDate] [datetime2](7) NULL,
	[HitCount] [int] NOT NULL,
 CONSTRAINT [PK_ShortLink] PRIMARY KEY CLUSTERED 
(
	[ShortLinkId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[ServiceCode](
	[ServiceCodeId] [int] IDENTITY(1,1) NOT NULL,
	[CPTCode] [varchar](10) NOT NULL,
	[Description] [nvarchar](200) NOT NULL,
	[DefaultUnits] [int] NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ServiceCodeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[queueClaimsToBeSubmitted](
	[QueueSubmitId] [int] IDENTITY(1,1) NOT NULL,
	[ClaimId] [int] NOT NULL,
	[EdiDocumentId] [int] NOT NULL,
	[Status] [varchar](50) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[SentDate] [datetime2](7) NULL,
	[AcknowledgedDate] [datetime2](7) NULL,
	[ClearinghouseResponse] [nvarchar](max) NULL,
 CONSTRAINT [PK_queueClaimsToBeSubmitted] PRIMARY KEY CLUSTERED 
(
	[QueueSubmitId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[queueAuthDocsToBeSubmitted](
	[QueueSubmitId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[PriorAuthFormId] [int] NOT NULL,
	[TreatmentPlanAuthorizationId] [int] NOT NULL,
	[RecipientEmail] [nvarchar](200) NOT NULL,
	[Status] [varchar](20) NOT NULL,
	[SentDate] [datetime2](7) NULL,
	[SentFolderPath] [nvarchar](400) NULL,
	[AuthorizationNumber] [nvarchar](100) NULL,
	[ResponseNotes] [nvarchar](max) NULL,
	[ResolvedDate] [datetime2](7) NULL,
	[ResolvedBy] [nvarchar](100) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[QueueSubmitId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[ClaimPipelineEvent](
	[EventId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NULL,
	[ClaimId] [int] NULL,
	[QueueClaimId] [int] NULL,
	[EventType] [varchar](50) NOT NULL,
	[EventDate] [datetime2](7) NOT NULL,
	[DocumentId] [int] NULL,
	[Details] [nvarchar](max) NULL,
 CONSTRAINT [PK_ClaimPipelineEvent] PRIMARY KEY CLUSTERED 
(
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[BillingRuleLine](
	[BillingRuleLineId] [int] IDENTITY(1,1) NOT NULL,
	[BillingRuleId] [int] NOT NULL,
	[LineName] [nvarchar](100) NOT NULL,
	[CptCode] [nvarchar](20) NOT NULL,
	[Modifier1] [nvarchar](10) NULL,
	[Modifier2] [nvarchar](10) NULL,
	[UnitFormula] [nvarchar](200) NULL,
	[Rate] [decimal](18, 2) NULL,
	[SortOrder] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BillingRuleLineId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[BillingRuleField](
	[BillingRuleFieldId] [int] IDENTITY(1,1) NOT NULL,
	[BillingRuleId] [int] NOT NULL,
	[FieldName] [nvarchar](100) NOT NULL,
	[JsonPath] [nvarchar](500) NULL,
	[IsRequired] [bit] NOT NULL,
	[DefaultValue] [nvarchar](500) NULL,
	[ValidationRule] [nvarchar](500) NULL,
	[SortOrder] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BillingRuleFieldId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[BillingRule](
	[BillingRuleId] [int] IDENTITY(1,1) NOT NULL,
	[PublicId] [uniqueidentifier] NOT NULL,
	[RuleName] [nvarchar](100) NOT NULL,
	[ServiceCode] [nvarchar](20) NOT NULL,
	[MinutesPerUnit] [int] NOT NULL,
	[Rate] [decimal](18, 2) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Priority] [int] NOT NULL,
	[EffectiveDate] [datetime2](0) NOT NULL,
	[ExpirationDate] [datetime2](0) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_BillingRule] PRIMARY KEY CLUSTERED 
(
	[BillingRuleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_BillingRule_PublicId] UNIQUE NONCLUSTERED 
(
	[PublicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BillingInvoices](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [int] NOT NULL,
	[PatientName] [nvarchar](200) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Diagnosis](
	[DiagnosisId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[DiagnosisCode] [varchar](20) NOT NULL,
	[Description] [nvarchar](200) NULL,
	[IsPrimary] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DiagnosisId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApplicationLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[MessageTemplate] [nvarchar](max) NULL,
	[Level] [nvarchar](16) NULL,
	[TimeStamp] [datetime] NULL,
	[Exception] [nvarchar](max) NULL,
	[Properties] [nvarchar](max) NULL,
 CONSTRAINT [PK_ApplicationLogs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[DocumentExtraction](
	[DocumentExtractionId] [int] IDENTITY(1,1) NOT NULL,
	[DocumentId] [int] NOT NULL,
	[ExtractionType] [nvarchar](50) NOT NULL,
	[ModelName] [nvarchar](100) NULL,
	[RawText] [nvarchar](max) NULL,
	[ExtractedJson] [nvarchar](max) NULL,
	[ValidationErrors] [nvarchar](max) NULL,
	[Status] [nvarchar](50) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DocumentExtractionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Workbook](
	[WorkbookId] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NOT NULL,
	[WorkbookTypeId] [int] NOT NULL,
	[Status] [nvarchar](50) NOT NULL,
	[DueDate] [datetime2](7) NULL,
	[CompletedDate] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkbookId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[WorkbookType](
	[WorkbookTypeId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_WorkbookType] PRIMARY KEY CLUSTERED 
(
	[WorkbookTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_WorkbookType_Name] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[WorkbookQRule](
	[WorkbookQRuleId] [int] IDENTITY(1,1) NOT NULL,
	[WorkbookQId] [int] NOT NULL,
	[DocumentType] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkbookQRuleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[WorkbookQ](
	[WorkbookQId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[QueryKey] [varchar](100) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[WorkbookQId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[WorkbookDocument](
	[WorkbookDocumentId] [int] IDENTITY(1,1) NOT NULL,
	[WorkbookId] [int] NOT NULL,
	[DocumentId] [int] NOT NULL,
	[DocumentType] [nvarchar](50) NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_WorkbookDocument] PRIMARY KEY CLUSTERED 
(
	[WorkbookDocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[TreatmentPlanAuthorization](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CaseId] [int] NULL,
	[PatientName] [nvarchar](200) NULL,
	[PatientDOB] [date] NULL,
	[MemberId] [nvarchar](100) NULL,
	[InsuranceName] [nvarchar](200) NULL,
	[DiagnosisCode] [nvarchar](50) NULL,
	[ProviderName] [nvarchar](200) NULL,
	[ProviderNPI] [nvarchar](20) NULL,
	[AuthorizationNumber] [nvarchar](100) NULL,
	[DocumentJson] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[SourceDocumentId] [int] NULL,
	[JsonDocumentId] [int] NULL,
	[PriorAuthFormId] [int] NULL,
 CONSTRAINT [PK_TreatmentPlanAuthorization] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[PayerEDI](
	[PayerEDIId] [int] IDENTITY(1,1) NOT NULL,
	[PayerId] [int] NOT NULL,
	[PayerIdentifier] [nvarchar](50) NULL,
	[PayerIdentifierQualifier] [nvarchar](10) NULL,
	[ReceiverNameREMOVE] [nvarchar](200) NULL,
	[ReceiverIdREMOVE] [nvarchar](50) NULL,
	[ReceiverQualifierREMOVE] [nvarchar](10) NULL,
	[IsActive] [bit] NOT NULL,
	[Notes] [nvarchar](1000) NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[ModifiedBy] [nvarchar](100) NULL,
 CONSTRAINT [PK_PayerEDI] PRIMARY KEY CLUSTERED 
(
	[PayerEDIId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Document](
	[DocumentId] [int] IDENTITY(1,1) NOT NULL,
	[VersionId] [uniqueidentifier] NOT NULL,
	[CaseId] [int] NULL,
	[SessionId] [int] NULL,
	[WorkbookQId] [int] NULL,
	[DocumentType] [varchar](50) NOT NULL,
	[Title] [nvarchar](200) NULL,
	[FileName] [nvarchar](255) NULL,
	[ContentType] [varchar](100) NOT NULL,
	[FileData] [varbinary](max) NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[IsActive] [bit] NOT NULL,
	[CaseNumber] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[DocumentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApplicationLogDetails](
	[LogId] [int] NOT NULL,
	[Message] [nvarchar](max) NULL,
	[MessageTemplate] [nvarchar](max) NULL,
	[Exception] [nvarchar](max) NULL,
	[Properties] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[LogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[FeeSchedule](
	[FeeScheduleId] [int] IDENTITY(1,1) NOT NULL,
	[PayerId] [int] NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[EffectiveDate] [date] NOT NULL,
	[TerminationDate] [date] NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[ModifiedBy] [varchar](100) NULL,
 CONSTRAINT [PK_FeeSchedule] PRIMARY KEY CLUSTERED 
(
	[FeeScheduleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[Invoice](
	[InvoiceId] [int] IDENTITY(1,1) NOT NULL,
	[PublicId] [uniqueidentifier] NOT NULL,
	[InvoiceNumber] [nvarchar](50) NOT NULL,
	[CaseId] [int] NOT NULL,
	[SessionId] [int] NULL,
	[InvoiceDate] [datetime2](0) NOT NULL,
	[Status] [nvarchar](50) NOT NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[Notes] [nvarchar](max) NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[ModifiedBy] [nvarchar](100) NULL,
	[ModifiedDate] [datetime2](0) NULL,
	[IsActive] [bit] NOT NULL,
	[CaseNumber] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Invoice] PRIMARY KEY CLUSTERED 
(
	[InvoiceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Invoice_InvoiceNumber] UNIQUE NONCLUSTERED 
(
	[InvoiceNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Invoice_PublicId] UNIQUE NONCLUSTERED 
(
	[PublicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[ProviderStaff](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProviderId] [int] NOT NULL,
	[SourceDocumentId] [int] NULL,
	[JsonDocumentId] [int] NULL,
	[StaffCode] [nvarchar](50) NOT NULL,
	[FirstName] [nvarchar](100) NOT NULL,
	[LastName] [nvarchar](100) NOT NULL,
	[ProviderType] [nvarchar](50) NOT NULL,
	[Credential] [nvarchar](100) NULL,
	[Email] [nvarchar](200) NULL,
	[Phone] [nvarchar](50) NULL,
	[HireDate] [date] NULL,
	[TerminationDate] [date] NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[ModifiedDate] [datetime2](0) NULL,
 CONSTRAINT [PK_ProviderStaff] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[FeeScheduleLine](
	[FeeScheduleLineId] [int] IDENTITY(1,1) NOT NULL,
	[FeeScheduleId] [int] NOT NULL,
	[ProcedureCode] [varchar](10) NOT NULL,
	[Description] [varchar](255) NULL,
	[UnitType] [varchar](20) NOT NULL,
	[MinutesPerUnit] [int] NOT NULL,
	[AllowedAmount] [decimal](10, 2) NOT NULL,
	[EffectiveDate] [date] NULL,
	[TerminationDate] [date] NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[CreatedBy] [varchar](100) NULL,
	[ModifiedDate] [datetime2](7) NULL,
	[ModifiedBy] [varchar](100) NULL,
 CONSTRAINT [PK_FeeScheduleLine] PRIMARY KEY CLUSTERED 
(
	[FeeScheduleLineId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[PayerSubmissionConfiguration](
	[PayerSubmissionConfigurationId] [int] IDENTITY(1,1) NOT NULL,
	[PayerId] [int] NOT NULL,
	[PayerEDIId] [int] NOT NULL,
	[ClearinghouseName] [varchar](100) NOT NULL,
	[SubmissionEndpoint] [varchar](500) NULL,
	[SubmitterId] [varchar](100) NULL,
	[SenderIdQualifier] [varchar](10) NULL,
	[RequiresEnrollment] [bit] NOT NULL,
	[RequiresPayerSpecificEdits] [bit] NOT NULL,
	[IsProduction] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[EffectiveDate] [date] NULL,
	[TerminationDate] [date] NULL,
	[CreatedDate] [datetime2](7) NOT NULL,
	[SubmissionMethod] [varbinary](50) NULL,
 CONSTRAINT [PK_PayerSubmissionConfiguration] PRIMARY KEY CLUSTERED 
(
	[PayerSubmissionConfigurationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[InvoiceLine](
	[InvoiceLineId] [int] IDENTITY(1,1) NOT NULL,
	[PublicId] [uniqueidentifier] NOT NULL,
	[InvoiceId] [int] NOT NULL,
	[ServiceCode] [nvarchar](20) NOT NULL,
	[Description] [nvarchar](255) NULL,
	[Units] [decimal](10, 2) NOT NULL,
	[Rate] [decimal](18, 2) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[SessionId] [int] NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_InvoiceLine] PRIMARY KEY CLUSTERED 
(
	[InvoiceLineId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_InvoiceLine_PublicId] UNIQUE NONCLUSTERED 
(
	[PublicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [cases].[AuditInvoice](
	[AuditInvoiceId] [int] IDENTITY(1,1) NOT NULL,
	[PublicId] [uniqueidentifier] NOT NULL,
	[InvoiceId] [int] NOT NULL,
	[InvoiceLineId] [int] NULL,
	[CaseId] [int] NULL,
	[SessionId] [int] NULL,
	[BillingRuleId] [int] NULL,
	[AuditType] [nvarchar](50) NOT NULL,
	[AuditData] [nvarchar](max) NOT NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime2](0) NOT NULL,
	[CaseNumber] [nvarchar](50) NULL,
 CONSTRAINT [PK_AuditInvoice] PRIMARY KEY CLUSTERED 
(
	[AuditInvoiceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_AuditInvoice_PublicId] UNIQUE NONCLUSTERED 
(
	[PublicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
CREATE NONCLUSTERED INDEX [IX_Payer_IsActive] ON [cases].[Payer]
(
	[IsActive] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
CREATE NONCLUSTERED INDEX [IX_Payer_PayerCode] ON [cases].[Payer]
(
	[PayerCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Errors] ON [dbo].[ApplicationLogs]
(
	[TimeStamp] ASC
)
WHERE ([Level]='Error')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_Level_TimeStamp] ON [dbo].[ApplicationLogs]
(
	[Level] ASC,
	[TimeStamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_ApplicationLogs_TimeStamp] ON [dbo].[ApplicationLogs]
(
	[TimeStamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
CREATE NONCLUSTERED INDEX [IX_Document_CaseNumber] ON [cases].[Document]
(
	[CaseNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Invoice_CaseId] ON [cases].[Invoice]
(
	[CaseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Invoice_SessionId] ON [cases].[Invoice]
(
	[SessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_InvoiceLine_InvoiceId] ON [cases].[InvoiceLine]
(
	[InvoiceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_ClaimSession_SessionDocument_Active] ON [cases].[ClaimSession]
(
	[SessionDocumentId] ASC
)
WHERE ([IsActive]=(1))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_AuditInvoice_InvoiceId] ON [cases].[AuditInvoice]
(
	[InvoiceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [cases].[Payer] ADD  CONSTRAINT [DF_Payer_PublicId]  DEFAULT (newsequentialid()) FOR [PublicId]
GO
ALTER TABLE [cases].[Payer] ADD  CONSTRAINT [DF_Payer_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Payer] ADD  CONSTRAINT [DF_Payer_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[Patient] ADD  CONSTRAINT [DF_Patient_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[MyConstants] ADD  CONSTRAINT [DF_MyConstants_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Provider] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[Provider] ADD  CONSTRAINT [DF_Provider_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[ProjectorRule] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[ProjectorRule] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[PriorAuthForm] ADD  CONSTRAINT [DF_PriorAuthForm_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ('ZZ') FOR [SenderIdQualifier]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ('ZZ') FOR [ReceiverIdQualifier]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ('HC') FOR [FunctionalIdentifierCode]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ('005010X222A1') FOR [VersionIdentifier]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ('T') FOR [TestIndicator]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ((1)) FOR [NextIsaControlNumber]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ((1)) FOR [NextGsControlNumber]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ((1)) FOR [NextStControlNumber]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[PracticeConfiguration] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Pipeline] ADD  CONSTRAINT [DF_Pipeline_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[Pipeline] ADD  CONSTRAINT [DF_Pipeline_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Pipeline] ADD  CONSTRAINT [DF_Pipeline_DocumentId]  DEFAULT ((-999)) FOR [DocumentId]
GO
ALTER TABLE [cases].[TableFieldMap] ADD  DEFAULT (sysdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[ShortLink] ADD  CONSTRAINT [DF_ShortLink_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[ShortLink] ADD  CONSTRAINT [DF_ShortLink_HitCount]  DEFAULT ((0)) FOR [HitCount]
GO
ALTER TABLE [cases].[SessionSchedule] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[SessionSchedule] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Session] ADD  CONSTRAINT [DF_Session_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[ServiceCode] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[queueClaimsToBeSubmitted] ADD  CONSTRAINT [DF_queueClaimsToBeSubmitted_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[queueClaimsToBeCreated] ADD  CONSTRAINT [DF_queueClaimsToBeCreated_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[queueAuthDocsToBeSubmitted] ADD  DEFAULT ('authdocs@example.invalid') FOR [RecipientEmail]
GO
ALTER TABLE [cases].[queueAuthDocsToBeSubmitted] ADD  DEFAULT ('Pending') FOR [Status]
GO
ALTER TABLE [cases].[queueAuthDocsToBeSubmitted] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[ClaimPipelineEvent] ADD  CONSTRAINT [DF_ClaimPipelineEvent_EventDate]  DEFAULT (sysutcdatetime()) FOR [EventDate]
GO
ALTER TABLE [cases].[Claim] ADD  CONSTRAINT [DF_Claim_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[Claim] ADD  CONSTRAINT [DF_Claim_IsVoided]  DEFAULT ((0)) FOR [IsVoided]
GO
ALTER TABLE [cases].[Claim] ADD  CONSTRAINT [DF_Claim_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Case] ADD  CONSTRAINT [DF_Case_PublicId]  DEFAULT (newsequentialid()) FOR [PublicId]
GO
ALTER TABLE [cases].[Case] ADD  CONSTRAINT [DF_Case_Status]  DEFAULT ('Open') FOR [Status]
GO
ALTER TABLE [cases].[Case] ADD  CONSTRAINT [DF_Case_OpenedDate]  DEFAULT (CONVERT([date],sysutcdatetime())) FOR [OpenedDate]
GO
ALTER TABLE [cases].[Case] ADD  CONSTRAINT [DF_Case_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Case] ADD  CONSTRAINT [DF_Case_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[BillingRuleLine] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [cases].[BillingRuleField] ADD  DEFAULT ((0)) FOR [IsRequired]
GO
ALTER TABLE [cases].[BillingRuleField] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [cases].[BillingRule] ADD  CONSTRAINT [DF_BillingRule_PublicId]  DEFAULT (newsequentialid()) FOR [PublicId]
GO
ALTER TABLE [cases].[BillingRule] ADD  CONSTRAINT [DF_BillingRule_Priority]  DEFAULT ((1)) FOR [Priority]
GO
ALTER TABLE [cases].[BillingRule] ADD  CONSTRAINT [DF_BillingRule_EffectiveDate]  DEFAULT (sysutcdatetime()) FOR [EffectiveDate]
GO
ALTER TABLE [cases].[BillingRule] ADD  CONSTRAINT [DF_BillingRule_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[BillingRule] ADD  CONSTRAINT [DF_BillingRule_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[BillingInvoices] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [cases].[Diagnosis] ADD  DEFAULT ((0)) FOR [IsPrimary]
GO
ALTER TABLE [cases].[Assessment] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[DocumentExtraction] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Workbook] ADD  DEFAULT ('Pending') FOR [Status]
GO
ALTER TABLE [cases].[Workbook] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Workbook] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[WorkbookType] ADD  CONSTRAINT [DF_WorkbookType_IsRequired]  DEFAULT ((1)) FOR [IsRequired]
GO
ALTER TABLE [cases].[WorkbookType] ADD  CONSTRAINT [DF_WorkbookType_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[WorkbookType] ADD  CONSTRAINT [DF_WorkbookType_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[WorkbookQ] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[WorkbookQ] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[WorkbookDocument] ADD  CONSTRAINT [DF_WorkbookDocument_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[WorkbookDocument] ADD  CONSTRAINT [DF_WorkbookDocument_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[TreatmentPlanAuthorization] ADD  CONSTRAINT [DF_TreatmentPlanAuthorization_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[PayerEDI] ADD  CONSTRAINT [DF_PayerEDI_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[PayerEDI] ADD  CONSTRAINT [DF_PayerEDI_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Document] ADD  DEFAULT (newid()) FOR [VersionId]
GO
ALTER TABLE [cases].[Document] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Document] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[FeeSchedule] ADD  CONSTRAINT [DF_FeeSchedule_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[FeeSchedule] ADD  CONSTRAINT [DF_FeeSchedule_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Invoice] ADD  CONSTRAINT [DF_Invoice_PublicId]  DEFAULT (newsequentialid()) FOR [PublicId]
GO
ALTER TABLE [cases].[Invoice] ADD  CONSTRAINT [DF_Invoice_InvoiceDate]  DEFAULT (sysutcdatetime()) FOR [InvoiceDate]
GO
ALTER TABLE [cases].[Invoice] ADD  CONSTRAINT [DF_Invoice_Status]  DEFAULT ('Draft') FOR [Status]
GO
ALTER TABLE [cases].[Invoice] ADD  CONSTRAINT [DF_Invoice_TotalAmount]  DEFAULT ((0)) FOR [TotalAmount]
GO
ALTER TABLE [cases].[Invoice] ADD  CONSTRAINT [DF_Invoice_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[Invoice] ADD  CONSTRAINT [DF_Invoice_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[ProviderStaff] ADD  CONSTRAINT [DF_ProviderStaff_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[ProviderStaff] ADD  CONSTRAINT [DF_ProviderStaff_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[FeeScheduleLine] ADD  CONSTRAINT [DF_FeeScheduleLine_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[FeeScheduleLine] ADD  CONSTRAINT [DF_FeeScheduleLine_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] ADD  CONSTRAINT [DF_PayerSubmissionConfiguration_RequiresEnrollment]  DEFAULT ((0)) FOR [RequiresEnrollment]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] ADD  CONSTRAINT [DF_PayerSubmissionConfiguration_RequiresPayerSpecificEdits]  DEFAULT ((1)) FOR [RequiresPayerSpecificEdits]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] ADD  CONSTRAINT [DF_PayerSubmissionConfiguration_IsProduction]  DEFAULT ((0)) FOR [IsProduction]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] ADD  CONSTRAINT [DF_PayerSubmissionConfiguration_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] ADD  CONSTRAINT [DF_PayerSubmissionConfiguration_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[InvoiceLine] ADD  CONSTRAINT [DF_InvoiceLine_PublicId]  DEFAULT (newsequentialid()) FOR [PublicId]
GO
ALTER TABLE [cases].[InvoiceLine] ADD  CONSTRAINT [DF_InvoiceLine_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[InvoiceLine] ADD  CONSTRAINT [DF_InvoiceLine_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[ClaimSession] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[ClaimSession] ADD  CONSTRAINT [DF_ClaimSession_Status]  DEFAULT ('Included') FOR [SessionStatus]
GO
ALTER TABLE [cases].[ClaimSession] ADD  CONSTRAINT [DF_ClaimSession_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [cases].[AuditInvoice] ADD  CONSTRAINT [DF_AuditInvoice_PublicId]  DEFAULT (newsequentialid()) FOR [PublicId]
GO
ALTER TABLE [cases].[AuditInvoice] ADD  CONSTRAINT [DF_AuditInvoice_CreatedDate]  DEFAULT (sysutcdatetime()) FOR [CreatedDate]
GO
ALTER TABLE [cases].[WorkbookDocument]  WITH CHECK ADD  CONSTRAINT [FK_WorkbookDocument_Workbook] FOREIGN KEY([WorkbookId])
REFERENCES [cases].[Workbook] ([WorkbookId])
GO
ALTER TABLE [cases].[WorkbookDocument] CHECK CONSTRAINT [FK_WorkbookDocument_Workbook]
GO
ALTER TABLE [cases].[TreatmentPlanAuthorization]  WITH CHECK ADD  CONSTRAINT [FK_TreatmentPlanAuthorization_PriorAuthForm] FOREIGN KEY([PriorAuthFormId])
REFERENCES [cases].[PriorAuthForm] ([Id])
GO
ALTER TABLE [cases].[TreatmentPlanAuthorization] CHECK CONSTRAINT [FK_TreatmentPlanAuthorization_PriorAuthForm]
GO
ALTER TABLE [cases].[PayerEDI]  WITH CHECK ADD  CONSTRAINT [FK_PayerEDI_Payer] FOREIGN KEY([PayerId])
REFERENCES [cases].[Payer] ([PayerId])
GO
ALTER TABLE [cases].[PayerEDI] CHECK CONSTRAINT [FK_PayerEDI_Payer]
GO
ALTER TABLE [cases].[Document]  WITH CHECK ADD  CONSTRAINT [FK_Document_Case] FOREIGN KEY([CaseId])
REFERENCES [cases].[Case] ([CaseId])
GO
ALTER TABLE [cases].[Document] CHECK CONSTRAINT [FK_Document_Case]
GO
ALTER TABLE [dbo].[ApplicationLogDetails]  WITH CHECK ADD FOREIGN KEY([LogId])
REFERENCES [dbo].[ApplicationLogs] ([Id])
GO
ALTER TABLE [cases].[FeeSchedule]  WITH CHECK ADD  CONSTRAINT [FK_FeeSchedule_Payer] FOREIGN KEY([PayerId])
REFERENCES [cases].[Payer] ([PayerId])
GO
ALTER TABLE [cases].[FeeSchedule] CHECK CONSTRAINT [FK_FeeSchedule_Payer]
GO
ALTER TABLE [cases].[Invoice]  WITH CHECK ADD  CONSTRAINT [FK_Invoice_Case] FOREIGN KEY([CaseId])
REFERENCES [cases].[Case] ([CaseId])
GO
ALTER TABLE [cases].[Invoice] CHECK CONSTRAINT [FK_Invoice_Case]
GO
ALTER TABLE [cases].[QueueClaimSession]  WITH CHECK ADD  CONSTRAINT [FK_QueueClaimSession_Queue] FOREIGN KEY([QueueClaimId])
REFERENCES [cases].[queueClaimsToBeCreated] ([QueueClaimId])
GO
ALTER TABLE [cases].[QueueClaimSession] CHECK CONSTRAINT [FK_QueueClaimSession_Queue]
GO
ALTER TABLE [cases].[ProviderStaff]  WITH CHECK ADD  CONSTRAINT [FK_ProviderStaff_JsonDocument] FOREIGN KEY([JsonDocumentId])
REFERENCES [cases].[Document] ([DocumentId])
GO
ALTER TABLE [cases].[ProviderStaff] CHECK CONSTRAINT [FK_ProviderStaff_JsonDocument]
GO
ALTER TABLE [cases].[ProviderStaff]  WITH CHECK ADD  CONSTRAINT [FK_ProviderStaff_Provider] FOREIGN KEY([ProviderId])
REFERENCES [cases].[Provider] ([Id])
GO
ALTER TABLE [cases].[ProviderStaff] CHECK CONSTRAINT [FK_ProviderStaff_Provider]
GO
ALTER TABLE [cases].[ProviderStaff]  WITH CHECK ADD  CONSTRAINT [FK_ProviderStaff_SourceDocument] FOREIGN KEY([SourceDocumentId])
REFERENCES [cases].[Document] ([DocumentId])
GO
ALTER TABLE [cases].[ProviderStaff] CHECK CONSTRAINT [FK_ProviderStaff_SourceDocument]
GO
ALTER TABLE [cases].[FeeScheduleLine]  WITH CHECK ADD  CONSTRAINT [FK_FeeScheduleLine_FeeSchedule] FOREIGN KEY([FeeScheduleId])
REFERENCES [cases].[FeeSchedule] ([FeeScheduleId])
GO
ALTER TABLE [cases].[FeeScheduleLine] CHECK CONSTRAINT [FK_FeeScheduleLine_FeeSchedule]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration]  WITH CHECK ADD  CONSTRAINT [FK_PayerSubmissionConfiguration_Payer] FOREIGN KEY([PayerId])
REFERENCES [cases].[Payer] ([PayerId])
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] CHECK CONSTRAINT [FK_PayerSubmissionConfiguration_Payer]
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration]  WITH CHECK ADD  CONSTRAINT [FK_PayerSubmissionConfiguration_PayerEDI] FOREIGN KEY([PayerEDIId])
REFERENCES [cases].[PayerEDI] ([PayerEDIId])
GO
ALTER TABLE [cases].[PayerSubmissionConfiguration] CHECK CONSTRAINT [FK_PayerSubmissionConfiguration_PayerEDI]
GO
ALTER TABLE [cases].[InvoiceLine]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLine_Invoice] FOREIGN KEY([InvoiceId])
REFERENCES [cases].[Invoice] ([InvoiceId])
GO
ALTER TABLE [cases].[InvoiceLine] CHECK CONSTRAINT [FK_InvoiceLine_Invoice]
GO
ALTER TABLE [cases].[ClaimSession]  WITH CHECK ADD  CONSTRAINT [FK_ClaimSession_Claim] FOREIGN KEY([ClaimId])
REFERENCES [cases].[Claim] ([ClaimId])
GO
ALTER TABLE [cases].[ClaimSession] CHECK CONSTRAINT [FK_ClaimSession_Claim]
GO
ALTER TABLE [cases].[ClaimSession]  WITH CHECK ADD  CONSTRAINT [FK_ClaimSession_Document] FOREIGN KEY([SessionDocumentId])
REFERENCES [cases].[Document] ([DocumentId])
GO
ALTER TABLE [cases].[ClaimSession] CHECK CONSTRAINT [FK_ClaimSession_Document]
GO
ALTER TABLE [cases].[AuditInvoice]  WITH CHECK ADD  CONSTRAINT [FK_AuditInvoice_Invoice] FOREIGN KEY([InvoiceId])
REFERENCES [cases].[Invoice] ([InvoiceId])
GO
ALTER TABLE [cases].[AuditInvoice] CHECK CONSTRAINT [FK_AuditInvoice_Invoice]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [cases].[usp_Workbook_InitializeForCase]
    @CaseId INT,
    @CreatedBy NVARCHAR(100)
	--EXEC [cases].[usp_Workbook_InitializeForCase]  @CaseId = 1, @CreatedBy = 'system'
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH SourceTypes AS
    (
        SELECT
            WorkbookTypeId,
            Name
        FROM [cases].[WorkbookType]
        WHERE IsActive = 1
    )
    INSERT INTO [cases].[Workbook]
    (
        CaseId,
        WorkbookTypeId,
        Status,
        CreatedBy,
        IsActive
    )
    SELECT
        @CaseId,
        st.WorkbookTypeId,
        'Pending',
        @CreatedBy,
        1
    FROM SourceTypes st
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [cases].[Workbook] w
        WHERE w.CaseId = @CaseId
          AND w.WorkbookTypeId = st.WorkbookTypeId
    );
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [cases].[usp_Pipeline_GetAll]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PipelineId,
        Name,
        Description,
        --TemplateJson,
       -- ParamsSchema,
		DocumentId
    FROM [cases].[Pipeline]
    WHERE IsActive = 1
    ORDER BY Name;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [cases].[usp_Payer_Upsert]
    @PublicId UNIQUEIDENTIFIER = NULL,
    @PayerName NVARCHAR(200),
    @PayerCode NVARCHAR(50) = NULL,
    @PayerType NVARCHAR(50) = NULL,
    @StateCode NVARCHAR(10) = NULL,
    @PhoneNumber NVARCHAR(50) = NULL,
    @Website NVARCHAR(500) = NULL,
    @PayerIdentifier NVARCHAR(100) = NULL,
    @EDIReceiverId NVARCHAR(100) = NULL,
    @ClaimsSubmissionMethod NVARCHAR(50) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @UserName NVARCHAR(100),
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- If CSV doesn't provide PublicId, generate one
    --IF @PublicId IS NULL
        --SET @PublicId = NEWSEQUENTIALID();

    DECLARE @ExistingPayerId INT;

    SELECT @ExistingPayerId = PayerId
    FROM [cases].[Payer]
    WHERE PublicId = @PublicId;

    IF @ExistingPayerId IS NULL
    BEGIN
        INSERT INTO [cases].[Payer]
        (
            PublicId,
            PayerCode,
            PayerName,
            PayerType,
            StateCode,
            PhoneNumber,
            Website,
            PayerIdentifier,
            EDIReceiverId,
            ClaimsSubmissionMethod,
            Notes,
            CreatedBy,
            IsActive
        )
        VALUES
        (
            ISNULL(@PublicId, NEWID()),
            @PayerCode,
            @PayerName,
            @PayerType,
            @StateCode,
            @PhoneNumber,
            @Website,
            @PayerIdentifier,
            @EDIReceiverId,
            @ClaimsSubmissionMethod,
            @Notes,
            @UserName,
            ISNULL(@IsActive, 1)
        );

        SELECT @PublicId AS PublicId, SCOPE_IDENTITY() AS PayerId;
    END
    ELSE
    BEGIN
        UPDATE [cases].[Payer]
        SET
            PayerCode = @PayerCode,
            PayerName = @PayerName,
            PayerType = @PayerType,
            StateCode = @StateCode,
            PhoneNumber = @PhoneNumber,
            Website = @Website,
            PayerIdentifier = @PayerIdentifier,
            EDIReceiverId = @EDIReceiverId,
            ClaimsSubmissionMethod = @ClaimsSubmissionMethod,
            Notes = @Notes,
            ModifiedDate = SYSUTCDATETIME(),
            ModifiedBy = @UserName,
            IsActive = ISNULL(@IsActive, IsActive)
        WHERE PublicId = @PublicId;

        SELECT @PublicId AS PublicId, @ExistingPayerId AS PayerId;
    END
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [cases].[usp_SubmitAuthDocs]
(
    @CaseId                       INT,
    @PriorAuthFormId              INT,
    @TreatmentPlanAuthorizationId INT,
    @CreatedBy                    NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO cases.queueAuthDocsToBeSubmitted
        (CaseId, PriorAuthFormId, TreatmentPlanAuthorizationId, Status, CreatedBy)
    VALUES (@CaseId, @PriorAuthFormId, @TreatmentPlanAuthorizationId, 'Pending', @CreatedBy);

    SELECT SCOPE_IDENTITY() AS QueueSubmitId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- exec [cases].[usp_Session_List] @CaseId = 5
CREATE PROCEDURE [cases].[usp_Session_List]
(
    @CaseId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [Id],[CaseId],[SessionDate],[SourceDocumentId],[JsonDocumentId]
    FROM cases.Session d
    WHERE d.CaseId = @CaseId
    ORDER BY d.CreatedDate DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [cases].[usp_DocumentExtraction_Insert]
(
    @DocumentId INT,
    @ExtractionType NVARCHAR(50),
    @ModelName NVARCHAR(100) = NULL,
    @RawText NVARCHAR(MAX) = NULL,
    @ExtractedJson NVARCHAR(MAX) = NULL,
    @ValidationErrors NVARCHAR(MAX) = NULL,
    @Status NVARCHAR(50),
    @CreatedBy NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [cases].[DocumentExtraction]
    (
        DocumentId,
        ExtractionType,
        ModelName,
        RawText,
        ExtractedJson,
        ValidationErrors,
        Status,
        CreatedDate,
        CreatedBy
    )
    VALUES
    (
        @DocumentId,
        @ExtractionType,
        @ModelName,
        @RawText,
        @ExtractedJson,
        @ValidationErrors,
        @Status,
        SYSUTCDATETIME(),
        @CreatedBy
    );

    SELECT SCOPE_IDENTITY() AS DocumentExtractionId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [cases].[usp_GetCalendar]
(
    @CaseId    INT  = NULL,
    @StartDate DATE,
    @EndDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH DateSpine AS (
        SELECT @StartDate AS d
        UNION ALL
        SELECT DATEADD(DAY, 1, d) FROM DateSpine WHERE d < @EndDate
    )

    SELECT
        s.Id                                                    AS EventId,
        'Session'                                                AS EventType,
        s.CaseId                                                 AS CaseId,
        s.ClientName                                             AS ClientName,
        CONCAT(s.ClientName, ' — ', ISNULL(s.CPTCode, ''))       AS Title,
        DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), s.StartTime), CAST(s.SessionDate AS DATETIME2)) AS [Start],
        DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), s.EndTime),   CAST(s.SessionDate AS DATETIME2)) AS [End],
        s.SessionStatus                                          AS Status,
        COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName))), ''), p.ProviderName) AS Provider,
        s.Location                                               AS Location
    FROM   cases.Session s
    LEFT JOIN cases.Provider p ON p.Id = s.ProviderId
    WHERE  (@CaseId IS NULL OR s.CaseId = @CaseId)
      AND  s.SessionDate BETWEEN @StartDate AND @EndDate
      AND  s.StartTime IS NOT NULL
      AND  s.EndTime   IS NOT NULL
      AND  (s.SessionStatus <> 'Projected' OR s.SessionStatus IS NULL OR s.SessionDate >= CAST(SYSUTCDATETIME() AS DATE))

    UNION ALL

    SELECT
        -(ss.Id * 100000 + DATEDIFF(DAY, '2000-01-01', ds.d))    AS EventId,
        'ProjectedSession'                                       AS EventType,
        ss.CaseId                                                AS CaseId,
        COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(pa.FirstName, ' ', pa.LastName))), ''), c.CaseNumber) AS ClientName,
        CONCAT(COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(pa.FirstName, ' ', pa.LastName))), ''), c.CaseNumber),
               ' — ', ISNULL(ss.CPTCode, ''))                    AS Title,
        DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), ss.StartTime), CAST(ds.d AS DATETIME2)) AS [Start],
        DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), ss.EndTime),   CAST(ds.d AS DATETIME2)) AS [End],
        'Projected'                                              AS Status,
        COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(pr.FirstName, ' ', pr.LastName))), ''), pr.ProviderName) AS Provider,
        NULL                                                     AS Location
    FROM   cases.SessionSchedule ss
    JOIN   DateSpine ds
      ON   ds.d >= CAST(SYSUTCDATETIME() AS DATE)
      AND  CASE (DATEDIFF(DAY, 0, ds.d) % 7)
               WHEN 0 THEN 'Mon' WHEN 1 THEN 'Tue' WHEN 2 THEN 'Wed' WHEN 3 THEN 'Thu'
               WHEN 4 THEN 'Fri' WHEN 5 THEN 'Sat' WHEN 6 THEN 'Sun' END = ss.DayOfWeek
    JOIN   cases.[Case] c ON c.CaseId = ss.CaseId
    LEFT JOIN cases.Patient pa ON pa.CaseId = ss.CaseId
    LEFT JOIN cases.Provider pr ON pr.Id = ss.ProviderId
    WHERE  ss.IsActive = 1
      AND  (@CaseId IS NULL OR ss.CaseId = @CaseId)
      AND  NOT EXISTS (
               SELECT 1 FROM cases.Session rs
               WHERE  rs.CaseId = ss.CaseId AND rs.SessionDate = ds.d
           )

    ORDER BY [Start]
    OPTION (MAXRECURSION 100);
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--  exec [cases].[usp_GetConstant] @Key = 'Pipeline837P.Definition.DocumentId'
CREATE   PROCEDURE [cases].[usp_GetConstant]
(
    @Key VARCHAR(200)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        MyConstantId,
        [Key],
        [Value],
        [Type]
    FROM [cases].[MyConstants]
    WHERE [Key] = @Key;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [cases].[usp_GetAuthDocsQueue]
(
    @CaseId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT QueueSubmitId, CaseId, PriorAuthFormId, TreatmentPlanAuthorizationId,
           RecipientEmail, Status, SentDate, SentFolderPath, AuthorizationNumber,
           ResponseNotes, ResolvedDate, ResolvedBy, CreatedDate, CreatedBy
    FROM cases.queueAuthDocsToBeSubmitted
    WHERE (@CaseId IS NULL OR CaseId = @CaseId)
    ORDER BY CreatedDate DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- exec [cases].[ResolveCaseFromDocuments] @CaseNumber = 'CASE-0000000001'
CREATE   PROCEDURE [cases].[ResolveCaseFromDocuments]
    @CaseNumber NVARCHAR(50),
    @ClientId INT = NULL,
    @Title NVARCHAR(200) = NULL,
    @CreatedBy NVARCHAR(100) = 'system',
    @Description NVARCHAR(MAX) = NULL,
    @Priority NVARCHAR(25) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CaseId INT;
	DECLARE @DocumentsLinked INT = 0;

    IF @CaseNumber IS NULL
        RETURN;

    BEGIN TRAN;

    -- 1. Find existing case
    SELECT @CaseId = CaseId
    FROM [cases].[Case]
    WHERE CaseNumber = @CaseNumber;

    -- 2. Create placeholder case if missing
    IF @CaseId IS NULL
    BEGIN
        INSERT INTO [cases].[Case]
        (
            CaseNumber,
            Title,
            Description,
            Status,
            Priority,
            ClientId,
            CreatedBy
        )
        VALUES
        (
            @CaseNumber,
            ISNULL(@Title, 'Unknown Case'),
            @Description,
            'Open',
            @Priority,
            ISNULL(@ClientId, 0),
            @CreatedBy
        );

        SET @CaseId = SCOPE_IDENTITY();
    END

    -- 3. Link documents
    UPDATE d
    SET d.CaseId = @CaseId
    FROM [cases].[Document] d
    WHERE d.CaseNumber = @CaseNumber
      AND d.CaseId IS NULL;

	-- MUST capture @@ROWCOUNT immediately
    SET @DocumentsLinked = @@ROWCOUNT;

    COMMIT TRAN;

    -- 4. Return summary
    SELECT
        @CaseId AS CaseId,
        @CaseNumber AS CaseNumber,
        @DocumentsLinked AS DocumentsLinked;
END
/**************
BEGIN TRAN;

DECLARE @SourceDocumentId INT = 6;
DECLARE @TargetDocumentId INT = 4;

UPDATE d_target
SET d_target.FileData = d_source.FileData
FROM [cases].[Document] d_target
JOIN [cases].[Document] d_source
    ON d_source.DocumentId = @SourceDocumentId
WHERE d_target.DocumentId = @TargetDocumentId
  AND d_source.FileData IS NOT NULL;

-- Uncomment ONE of these when ready:

-- COMMIT TRAN;
-- ROLLBACK TRAN;
****************/
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- exec  [cases].[usp_Get837P]  @CaseId  = 7, @SessionId = 2

CREATE   PROCEDURE [cases].[usp_Get837P]
(
    @CaseId     INT,
    @SessionId  INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------------
    -- Session
    ------------------------------------------------------------------
    SELECT *
    FROM [cases].[Session]
    WHERE SessionId = @SessionId
      AND CaseId = @CaseId;

    ------------------------------------------------------------------
    -- Case
    ------------------------------------------------------------------
    SELECT *
    FROM [cases].[Case]
    WHERE CaseId = @CaseId;
	/*****************/
    ------------------------------------------------------------------
    -- Payer / Coverage
    ------------------------------------------------------------------
    SELECT
        ic.*,
        p.*
    FROM [cases].[InsuranceCoverage] ic 
		INNER JOIN [cases].[Payer] p ON p.PayerId = ic.PayerId
    WHERE ic.CaseId = 0; -- @CaseId;

	 --SELECT
  --      --ic.*,
  --      p.*
  --  FROM [cases].[Payer] p where p.[PayerCode] = 'CIGNA'


    ------------------------------------------------------------------
    -- Authorization
    ------------------------------------------------------------------
    SELECT *
    FROM [cases].[Authorization]
    WHERE CaseId = @CaseId
      AND GETDATE() BETWEEN StartDate AND EndDate;
	  
    ------------------------------------------------------------------
    -- Diagnosis
    ------------------------------------------------------------------
    SELECT *
    FROM [cases].[Diagnosis]
    WHERE CaseId = 0 --@CaseId
    ORDER BY IsPrimary DESC, DiagnosisId;
	/*****************/
    ------------------------------------------------------------------
    -- Billing Provider
    ------------------------------------------------------------------
    SELECT *
    FROM [cases].[Provider]
    WHERE IsActive = 1;

	/****************
    ------------------------------------------------------------------
    -- Rendering Provider
    ------------------------------------------------------------------
    SELECT rp.*
    FROM [cases].[RenderingProvider] rp
    INNER JOIN [cases].[Session] s ON s.RenderingProviderId = rp.RenderingProviderId
    WHERE s.SessionId = @SessionId;
	
    ------------------------------------------------------------------
    -- Service Codes
    ------------------------------------------------------------------
    SELECT sc.*
    FROM [cases].[ServiceCode] sc
    INNER JOIN [cases].[Session] s ON s.ServiceCodeId = sc.ServiceCodeId
    WHERE s.SessionId = @SessionId;
		****************/

	------------------------------------------------------------------
    -- Assessment Codes
    ------------------------------------------------------------------
	SELECT aa.*
	FROM [cases].[Assessment] aa
	WHERE CaseId = @CaseId
	--ORDER BY AssessmentDate DESC;


	-- right side
	SELECT d.FileData
	FROM [cases].[Document] d
	INNER JOIN [cases].[MyConstants] c ON d.DocumentId = CAST(c.[Value] AS INT)
	WHERE c.[Key] = 'Pipeline837P.Definition.DocumentId';

	--http://localhost:5173/api/getDocument?docId=174
	-- exec [cases].[usp_GetConstant] @Key = 'Pipeline837P.Definition.DocumentId'
	--SELECT [FileData] FROM [CaseManagement].[cases].[Document] where  [DocumentId] = 174
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- exec [cases].[usp_Case_GetDocuments] @CaseId = 5
CREATE PROCEDURE [cases].[usp_Case_GetDocuments]
(
    @CaseId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DocumentId,
        d.CaseId,
		d.CaseNumber,
        --d.WorkbookQId,
        --d.SessionId,
		d.Title,
        d.DocumentType,
       
        --d.FileName,
        --d.ContentType,
        d.CreatedDate
        --d.CreatedBy
    FROM cases.Document d
    WHERE d.CaseId = @CaseId
      AND d.IsActive = 1
    ORDER BY d.CreatedDate DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Template: copy and rename for each doc type (usp_Authorization_Resolve, etc.)
-- Called by DocResolveStep via ResolveDocAsync.
-- Signature: @DocId (JSON doc), @CaseId, @SessionId, @SrcDocId (original source doc)

-- exec [cases].[usp_Assessment_Resolve] @DocId = 208, @CaseId = 7, @SessionId = NULL,  @SrcDocId = NULL

CREATE   PROCEDURE [cases].[usp_Assessment_Resolve]
(
    @DocId     INT,
    @CaseId    INT,
    @SessionId INT = NULL,
    @SrcDocId  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [cases].[Document] WHERE DocumentId = @DocId AND IsActive = 1)
        THROW 50001, 'JSON document not found or inactive.', 1;

    IF EXISTS (SELECT 1 FROM [cases].[Assessment] WHERE CaseId = @CaseId)
    BEGIN
        UPDATE [cases].[Assessment]
        SET JsonDocumentId    = @DocId,
            SourceDocumentId  = @SrcDocId
        WHERE CaseId = @CaseId;
    END
    ELSE
    BEGIN
        INSERT INTO [cases].[Assessment] (CaseId, JsonDocumentId, SourceDocumentId)
        VALUES (@CaseId, @DocId, @SrcDocId);
    END
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [cases].[usp_GetClaimQueue]
(
    @CaseId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        q.QueueClaimId,
        q.CaseId,
        q.Status,
        q.CreatedDate,
        q.SpecDocumentId,
        STRING_AGG(CAST(qcs.SessionDocumentId AS VARCHAR(20)), ',') AS SessionDocumentIds
    FROM cases.queueClaimsToBeCreated q
    LEFT JOIN cases.QueueClaimSession qcs ON qcs.QueueClaimId = q.QueueClaimId
    WHERE (@CaseId IS NULL OR q.CaseId = @CaseId)
    GROUP BY q.QueueClaimId, q.CaseId, q.Status, q.CreatedDate, q.SpecDocumentId
    ORDER BY q.CreatedDate DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 
/************
DECLARE @bin VARBINARY(MAX);
SET @bin = CONVERT(VARBINARY(MAX), 'hello there');
exec  [cases].[usp_Document_Save] @CaseId = -9, @DocumentType = 'TEST', @ContentType='text/html',  @FileData = @bin;
--CAST('hello there' AS VARBINARY(MAX));--CONVERT(VARBINARY(MAX), 'hello there');
********/
CREATE   PROCEDURE [cases].[usp_Document_Save]
    @CaseId        INT             = NULL,
    @CaseNumber    NVARCHAR(50)    = NULL,
    @SessionId     INT             = NULL,
    @WorkbookQId   INT             = NULL,
    @DocumentType  VARCHAR(50)     = NULL,
    @Title         NVARCHAR(200)   = NULL,
    @FileName      NVARCHAR(255)   = NULL,
    @ContentType   VARCHAR(100)    = NULL,
    @FileData      VARBINARY(MAX),
    @CreatedBy     NVARCHAR(100)   = NULL,
    @DocumentId    INT             = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @DocumentId IS NOT NULL
       AND EXISTS (SELECT 1 FROM [cases].[Document] WHERE DocumentId = @DocumentId AND IsActive = 1)
    BEGIN
        UPDATE [cases].[Document]
        SET
            VersionId    = NEWID(),
			DocumentType = COALESCE(@DocumentType, DocumentType),
			Title        = COALESCE(@Title, Title),
			FileName     = COALESCE(@FileName, FileName),
			ContentType  = COALESCE(@ContentType, ContentType),
			FileData     = COALESCE(@FileData, FileData),
            CreatedDate  = SYSUTCDATETIME(),
            CreatedBy    = COALESCE(@CreatedBy, CreatedBy)
        WHERE DocumentId = @DocumentId;
        -- @DocumentId is already set; OUTPUT returns it unchanged
    END
    ELSE
    BEGIN
        INSERT INTO [cases].[Document]
        (
            VersionId, CaseId, CaseNumber, SessionId, WorkbookQId,
            DocumentType, Title, FileName, ContentType, FileData,
            CreatedDate, CreatedBy, IsActive
        )
        VALUES
        (
            NEWID(), @CaseId, @CaseNumber, @SessionId, @WorkbookQId,
            @DocumentType, @Title, @FileName, @ContentType, @FileData,
            SYSUTCDATETIME(), @CreatedBy, 1
        );

        SET @DocumentId = SCOPE_IDENTITY();
    END
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [cases].[usp_Document_List]
(
    @DocumentType  VARCHAR(50)  = NULL,
    @CaseId        INT          = NULL,
    @IsActive      BIT          = 1
)
AS
-- exec [cases].[usp_Document_List] @DocumentType = 'Other'
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DocumentId,
        --d.CaseId,
        --d.CaseNumber,
        --d.WorkbookQId,
        --d.SessionId,
        --d.DocumentType,
        d.Title,
        d.FileName,
        d.ContentType,
        d.CreatedDate,
        d.CreatedBy
    FROM cases.Document d
    WHERE
        (@DocumentType IS NULL OR d.DocumentType = @DocumentType)
        AND (@CaseId    IS NULL OR d.CaseId       = @CaseId)
        AND (@IsActive  IS NULL OR d.IsActive      = @IsActive)
    ORDER BY d.CreatedDate DESC;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [cases].[usp_Document_GetById]
(
    @DocumentId INT
)
AS
/*
EXEC cases.[usp_Document_GetById] @DocumentId = 4
*/
BEGIN
    SET NOCOUNT ON;

    SELECT
        DocumentId,
        VersionId,
        CaseId,
        WorkbookQId,
        SessionId,
        DocumentType,
        Title,
        FileName,
        ContentType,
        FileData,
        CreatedDate,
        CreatedBy,
        IsActive
    FROM [cases].[Document]
    WHERE
        DocumentId = @DocumentId
        AND IsActive = 1;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [cases].[usp_Document_GetByContext]
(
    @DocumentId   INT           = NULL,
    @CaseId       INT           = NULL,
    @WorkbookQId  INT           = NULL,
    @SessionId    INT           = NULL,
    @DocumentType VARCHAR(100)  = NULL
)
AS
/*
EXEC cases.[usp_Document_GetByContext] @DocumentId = 4
EXEC cases.[usp_Document_GetByContext] @CaseId = 2, @DocumentType = 'IntakeForm'
EXEC cases.[usp_Document_GetByContext] @DocumentType = 'IntakeForm', @DocumentType = 'session'
*/
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        DocumentId,
        VersionId,
        CaseId,
        WorkbookQId,
        SessionId,
        DocumentType,
        Title,
        FileName,
        ContentType,
        FileData,
        CreatedDate,
        CreatedBy,
        IsActive
    FROM [cases].[Document]
    WHERE IsActive = 1
      AND (@DocumentId   IS NULL OR DocumentId   = @DocumentId)
      AND (@CaseId       IS NULL OR CaseId       = @CaseId)
      AND (@WorkbookQId  IS NULL OR WorkbookQId  = @WorkbookQId)
      AND (@SessionId    IS NULL OR SessionId    = @SessionId)
      AND (@DocumentType IS NULL OR DocumentType = @DocumentType)
    ORDER BY CreatedDate DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE     PROCEDURE [cases].[usp_WorkbookQ_GetDocuments]
(
    @CaseId INT,
    @WorkbookQId INT
)
AS
/*
EXEC cases.usp_WorkbookQ_GetDocuments @CaseId = 1, @WorkbookQId = 1001
*/
BEGIN
    SET NOCOUNT ON;

SELECT
    r.DocumentType,
    d.DocumentId,
    d.Title,
    d.ContentType
FROM cases.WorkbookQRule r
LEFT JOIN cases.Document d
    ON d.WorkbookQId = r.WorkbookQId
    AND d.DocumentType = r.DocumentType
    AND d.CaseId = @CaseId
    AND d.IsActive = 1
WHERE r.WorkbookQId = @WorkbookQId
    ORDER BY d.CreatedDate DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Enriches a just-inserted row by reading field values from a JSON document,
-- driven by the mapping registered in cases.TableFieldMap for the given table.
-- Called by usp_CaseTable_Resolve after INSERT. Silent no-op if no mapping exists.

-- exec [cases].[usp_TableFieldMap_Apply] @TableName = 'Session', @RowId = 42, @JsonDocId = 418

CREATE   PROCEDURE [cases].[usp_TableFieldMap_Apply]
(
    @TableName  SYSNAME,
    @RowId      INT,
    @JsonDocId  INT
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Look up latest mapping for this table
    DECLARE @MappingDocId INT;
    SELECT TOP 1 @MappingDocId = JsonDocId
    FROM   [cases].[TableFieldMap]
    WHERE  TableName = @TableName
    ORDER  BY Version DESC;

    IF @MappingDocId IS NULL RETURN;

    DECLARE @mappingContent VARCHAR(MAX);
    SELECT @mappingContent = CAST(FileData AS VARCHAR(MAX))
    FROM   [cases].[Document]
    WHERE  DocumentId = @MappingDocId AND IsActive = 1;

    IF @mappingContent IS NULL RETURN;

    DECLARE @srcContent VARCHAR(MAX);
    SELECT @srcContent = CAST(FileData AS VARCHAR(MAX))
    FROM   [cases].[Document]
    WHERE  DocumentId = @JsonDocId AND IsActive = 1;

    IF @srcContent IS NULL RETURN;

    DECLARE @tbl        NVARCHAR(258)  = N'[cases].' + QUOTENAME(@TableName);
    DECLARE @setClauses NVARCHAR(MAX)  = N'';
    DECLARE @col        NVARCHAR(128);
    DECLARE @path       NVARCHAR(512);
    DECLARE @datatype   NVARCHAR(50);
    DECLARE @colExpr    NVARCHAR(MAX);

    DECLARE fieldCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            JSON_VALUE(value, '$.column'),
            JSON_VALUE(value, '$.path'),
            JSON_VALUE(value, '$.datatype')
        FROM OPENJSON(@mappingContent, '$.fields');

    OPEN fieldCursor;
    FETCH NEXT FROM fieldCursor INTO @col, @path, @datatype;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Whitelist: reject unknown datatypes to prevent injection
        IF @datatype IS NOT NULL AND @datatype NOT IN (
            'date', 'datetime', 'datetime2', 'time',
            'int', 'bigint', 'smallint', 'tinyint',
            'decimal(10,2)', 'decimal(18,2)', 'decimal(18,4)',
            'float', 'bit',
            'nvarchar', 'nvarchar(max)'
        )
        BEGIN
            FETCH NEXT FROM fieldCursor INTO @col, @path, @datatype;
            CONTINUE;
        END

        DECLARE @pathEscaped NVARCHAR(512) = REPLACE(@path, '''', '''''');

        SET @colExpr = CASE
            WHEN @datatype IS NOT NULL
                THEN 'TRY_CAST(JSON_VALUE(@src, ''' + @pathEscaped + ''') AS ' + @datatype + ')'
            ELSE
                'JSON_VALUE(@src, ''' + @pathEscaped + ''')'
        END;

        IF LEN(@setClauses) > 0 SET @setClauses += N', ';
        SET @setClauses += QUOTENAME(@col) + N' = ' + @colExpr;

        FETCH NEXT FROM fieldCursor INTO @col, @path, @datatype;
    END

    CLOSE fieldCursor;
    DEALLOCATE fieldCursor;

    IF LEN(@setClauses) = 0 RETURN;

    DECLARE @sql NVARCHAR(MAX) =
        N'UPDATE ' + @tbl + N' SET ' + @setClauses + N' WHERE Id = @RowId';

    EXEC sp_executesql @sql,
        N'@src VARCHAR(MAX), @RowId INT',
        @src = @srcContent, @RowId = @RowId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 3. usp_SubmitClaim — logs QueueClaimSubmitted
CREATE PROCEDURE [cases].[usp_SubmitClaim]
(
    @CaseId             INT,
    @SessionDocumentIds NVARCHAR(MAX),
    @SpecDocumentId     INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO cases.queueClaimsToBeCreated
        (
            CaseId, Status, SpecDocumentId
        )
        VALUES
        (
            @CaseId, 'Pending', @SpecDocumentId
        );

        DECLARE @QueueClaimId INT = SCOPE_IDENTITY();

        INSERT INTO cases.QueueClaimSession (QueueClaimId, SessionDocumentId)
        SELECT @QueueClaimId, TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@SessionDocumentIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

        INSERT INTO cases.ClaimPipelineEvent (CaseId, QueueClaimId, EventType, DocumentId, Details)
        VALUES (@CaseId, @QueueClaimId, 'QueueClaimSubmitted', @SpecDocumentId, CONCAT('Sessions=', @SessionDocumentIds));

        COMMIT;

        SELECT @QueueClaimId AS QueueClaimId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [cases].[usp_SearchCases]
    @CaseId INT = NULL
	--EXEC cases.usp_SearchCases @CaseId = 5 -- @ClientId = 9
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM   cases.vw_CaseSearch
    WHERE  @CaseId IS NULL OR Id = @CaseId
    ORDER  BY Opened DESC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [cases].[usp_GetManifestJson]
    @IncludeInactive BIT = 0,   -- ProjectorRule: include inactive rows
    @AllVersions     BIT = 0    -- TableFieldMap: every version, not just the latest per table
AS
BEGIN
    SET NOCOUNT ON;

    WITH Docs AS (
        -- ProjectorRules: one row per projection doc and per rule doc
        SELECT 'ProjectorRules' AS Folder, r.Name, 'projection' AS Role, r.ProjectionDocumentId AS DocumentId
        FROM   [cases].[ProjectorRule] r
        WHERE  (@IncludeInactive = 1 OR r.IsActive = 1) AND r.ProjectionDocumentId > 0
        UNION ALL
        SELECT 'ProjectorRules', r.Name, 'rule', r.RuleDocumentId
        FROM   [cases].[ProjectorRule] r
        WHERE  (@IncludeInactive = 1 OR r.IsActive = 1) AND r.RuleDocumentId > 0

        UNION ALL
        -- FieldMaps: latest version per table, or every version
        SELECT 'FieldMaps', m.TableName, 'fieldmap', m.JsonDocId
        FROM  (SELECT TableName, JsonDocId,
                      ROW_NUMBER() OVER (PARTITION BY TableName ORDER BY Version DESC) AS rn
               FROM   [cases].[TableFieldMap]) m
        WHERE  @AllVersions = 1 OR m.rn = 1

        UNION ALL
        -- Constants: int constants that point at a real document; a doc under two keys is listed once
        SELECT 'Constants', k.[Key], 'constant', TRY_CAST(k.[Value] AS INT)
        FROM  (SELECT [Key], [Value],
                      ROW_NUMBER() OVER (PARTITION BY TRY_CAST([Value] AS INT) ORDER BY MyConstantId) AS rn
               FROM   [cases].[MyConstants]
               WHERE  [Type] = 'int'
                 AND  EXISTS (SELECT 1 FROM [cases].[Document] x WHERE x.DocumentId = TRY_CAST([Value] AS INT))) k
        WHERE  k.rn = 1
    )
    SELECT d.Folder, d.Name, d.Role, d.DocumentId, doc.ContentType
    FROM   Docs d
    LEFT JOIN [cases].[Document] doc ON doc.DocumentId = d.DocumentId
    ORDER  BY CASE d.Folder WHEN 'ProjectorRules' THEN 1 WHEN 'FieldMaps' THEN 2 ELSE 3 END, d.Name, d.Role;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [cases].[usp_Workbook_GetByCaseId]
    @CaseId INT
	--EXEC [cases].[usp_Workbook_GetByCaseId]  @CaseId = 1001
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wt.WorkbookTypeId,
        wt.Name AS WorkbookName,
        wt.Description,
        wt.DisplayOrder,

        w.WorkbookId,
        --w.CaseId,
		COALESCE(w.CaseId, 2) AS CaseId,
		'session.187.aba_session_editable.pdf' as fileName,
        w.Status,
        w.DueDate,
        w.CreatedDate,
        w.CompletedDate,

        CAST(CASE WHEN w.WorkbookId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsStarted,
        CAST(CASE WHEN ISNULL(doc.DocumentCount, 0) > 0 THEN 1 ELSE 0 END AS BIT) AS HasDocuments,
        ISNULL(doc.DocumentCount, 0) AS DocumentCount
    FROM [cases].[WorkbookType] wt
    LEFT JOIN [cases].[Workbook] w
        ON w.WorkbookTypeId = wt.WorkbookTypeId
       AND w.CaseId = @CaseId
       AND w.IsActive = 1
    OUTER APPLY
    (
        SELECT COUNT(*) AS DocumentCount
        FROM [cases].[WorkbookDocument] wd
        WHERE wd.WorkbookId = w.WorkbookId
          AND wd.IsActive = 1
    ) doc
    WHERE wt.IsActive = 1
    ORDER BY wt.DisplayOrder;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 
/****
DECLARE @RC int
DECLARE @ClaimId int
DECLARE @VoidReason nvarchar(500)

-- TODO: Set parameter values here.

EXECUTE @RC = [cases].[usp_VoidClaim] @ClaimId = 4, @VoidReason= 'void for testing.'
select @RC
GO


*****/
--
-- 5. usp_VoidClaim must also flip ClaimSession.IsActive = 0 for that claim's
--    rows, or the flag drifts out of sync on the very next void
CREATE   PROCEDURE [cases].[usp_VoidClaim]
(
    @ClaimId    INT,
    @VoidReason NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        ----------------------------------------------------
        -- Verify Claim Exists
        ----------------------------------------------------
        IF NOT EXISTS
        (
            SELECT 1
            FROM cases.Claim
            WHERE ClaimId = @ClaimId
        )
        BEGIN
            THROW 50010, 'Claim not found.', 1;
        END;

        ----------------------------------------------------
        -- Already Voided?
        ----------------------------------------------------
        IF EXISTS
        (
            SELECT 1
            FROM cases.Claim
            WHERE ClaimId = @ClaimId
              AND IsActive = 0
        )
        BEGIN
            THROW 50011, 'Claim is already voided.', 1;
        END;

        ----------------------------------------------------
        -- Void Claim
        ----------------------------------------------------
        UPDATE cases.Claim
        SET
            IsActive   = 0,
            IsVoided   = 1,
            Status     = 'Voided',
            VoidDate   = SYSUTCDATETIME(),
            VoidReason = @VoidReason
        WHERE ClaimId = @ClaimId;

        ----------------------------------------------------
        -- Release Sessions — let the filtered unique index
        -- allow these SessionDocumentIds to be re-claimed
        ----------------------------------------------------
        UPDATE cases.ClaimSession
        SET    IsActive = 0
        WHERE  ClaimId = @ClaimId;

        COMMIT;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK;

        THROW;

    END CATCH
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [cases].[usp_PriorAuthBundle_Resolve]
(
    @CaseId                 INT,
    @PriorAuthFormDocId     INT,
    @PriorAuthFormSrcDocId  INT,
    @TreatmentPlanDocId     INT,
    @TreatmentPlanSrcDocId  INT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM cases.Document WHERE DocumentId = @PriorAuthFormDocId AND IsActive = 1)
            THROW 50001, 'PriorAuthForm JSON document not found or inactive.', 1;
        IF NOT EXISTS (SELECT 1 FROM cases.Document WHERE DocumentId = @TreatmentPlanDocId AND IsActive = 1)
            THROW 50001, 'TreatmentPlanAuthorization JSON document not found or inactive.', 1;

        INSERT INTO cases.PriorAuthForm (CaseId, JsonDocumentId, SourceDocumentId)
        VALUES (@CaseId, @PriorAuthFormDocId, @PriorAuthFormSrcDocId);

        DECLARE @PriorAuthFormId INT = SCOPE_IDENTITY();

        EXEC cases.usp_TableFieldMap_Apply
            @TableName = 'PriorAuthForm', @RowId = @PriorAuthFormId, @JsonDocId = @PriorAuthFormDocId;

        INSERT INTO cases.TreatmentPlanAuthorization (CaseId, JsonDocumentId, SourceDocumentId, PriorAuthFormId)
        VALUES (@CaseId, @TreatmentPlanDocId, @TreatmentPlanSrcDocId, @PriorAuthFormId);

        DECLARE @TreatmentPlanAuthorizationId INT = SCOPE_IDENTITY();

        EXEC cases.usp_TableFieldMap_Apply
            @TableName = 'TreatmentPlanAuthorization', @RowId = @TreatmentPlanAuthorizationId, @JsonDocId = @TreatmentPlanDocId;

        COMMIT;

        SELECT @PriorAuthFormId AS PriorAuthFormId, @TreatmentPlanAuthorizationId AS TreatmentPlanAuthorizationId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [cases].[usp_NewCase_Assessment_Resolve]
(
    @SourceDocumentId    INT,
    @AssessmentJsonDocId INT,
    @CaseTitle           NVARCHAR(200) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM cases.Document WHERE DocumentId = @SourceDocumentId AND IsActive = 1)
            THROW 50001, 'Source document not found or inactive.', 1;
        IF NOT EXISTS (SELECT 1 FROM cases.Document WHERE DocumentId = @AssessmentJsonDocId AND IsActive = 1)
            THROW 50001, 'Assessment JSON document not found or inactive.', 1;
        IF EXISTS (SELECT 1 FROM cases.Document WHERE DocumentId = @SourceDocumentId AND CaseId IS NOT NULL)
            THROW 50003, 'Source document is already attached to a case.', 1;

        DECLARE @CaseNumber NVARCHAR(50);
        DECLARE @NewCaseId INT = NULL;
        DECLARE @Attempts INT = 0;
        WHILE @NewCaseId IS NULL AND @Attempts < 5
        BEGIN
            SET @CaseNumber = 'CASE-' + CAST(1000000000 + CAST(ABS(CHECKSUM(NEWID())) AS BIGINT) AS VARCHAR(20));
            IF NOT EXISTS (SELECT 1 FROM cases.[Case] WHERE CaseNumber = @CaseNumber)
            BEGIN
                INSERT INTO cases.[Case] (CaseNumber, Title, CreatedBy)
                VALUES (@CaseNumber, COALESCE(NULLIF(@CaseTitle, ''), 'Unknown Case'), 'new_patient_intake');
                SET @NewCaseId = SCOPE_IDENTITY();
            END
            SET @Attempts += 1;
        END
        IF @NewCaseId IS NULL
            THROW 50002, 'Could not generate a unique CaseNumber after 5 attempts.', 1;

        UPDATE cases.Document SET CaseId = @NewCaseId WHERE DocumentId = @SourceDocumentId;

        INSERT INTO cases.Assessment (CaseId, JsonDocumentId, SourceDocumentId)
        VALUES (@NewCaseId, @AssessmentJsonDocId, @SourceDocumentId);
        DECLARE @AssessmentId INT = SCOPE_IDENTITY();

        EXEC cases.usp_TableFieldMap_Apply
            @TableName = 'Assessment', @RowId = @AssessmentId, @JsonDocId = @AssessmentJsonDocId;

        COMMIT;

        SELECT @NewCaseId AS CaseId, @CaseNumber AS CaseNumber, @AssessmentId AS AssessmentId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- exec [cases].[usp_Invoice_List]
CREATE   PROCEDURE [cases].[usp_Invoice_List]
(
    @CaseId        INT = NULL,
    @Status        NVARCHAR(50) = NULL,
    @FromDate      DATETIME2(0) = NULL,
    @ToDate        DATETIME2(0) = NULL,
    @SessionId     INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.InvoiceId,
        i.PublicId,
        i.InvoiceNumber,

        i.CaseId,
        c.CaseNumber,
        c.Title AS CaseTitle,

        i.SessionId,

        i.InvoiceDate,
        i.Status,
        i.TotalAmount,

        i.CreatedBy,
        i.CreatedDate,

        (
            SELECT COUNT(*)
            FROM [cases].[InvoiceLine] il
            WHERE il.InvoiceId = i.InvoiceId
        ) AS LineCount

    FROM [cases].[Invoice] i
    INNER JOIN [cases].[Case] c
        ON c.CaseId = i.CaseId

    WHERE
        (@CaseId IS NULL OR i.CaseId = @CaseId)
        AND (@Status IS NULL OR i.Status = @Status)
        AND (@SessionId IS NULL OR i.SessionId = @SessionId)
        AND (@FromDate IS NULL OR i.InvoiceDate >= @FromDate)
        AND (@ToDate IS NULL OR i.InvoiceDate <= @ToDate)

    ORDER BY
        i.InvoiceDate DESC,
        i.InvoiceId DESC;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- exec  [cases].[usp_Invoice_GetDetails]  @InvoiceId = 3
CREATE   PROCEDURE [cases].[usp_Invoice_GetDetails]
(
    @InvoiceId INT
)
AS
BEGIN

    SET NOCOUNT ON;

    /* =========================================================
       Invoice Header
       ========================================================= */

    SELECT
        i.InvoiceId,
        i.PublicId,
        i.InvoiceNumber,

        i.CaseId,
        i.CaseNumber,

        c.Title              AS CaseTitle,
        c.Status             AS CaseStatus,

        i.SessionId,

        i.InvoiceDate,
        i.Status,

        i.TotalAmount,
        i.Notes,

        i.CreatedBy,
        i.CreatedDate,

        i.ModifiedBy,
        i.ModifiedDate,

        i.IsActive

    FROM [cases].[Invoice] i
    INNER JOIN [cases].[Case] c
        ON c.CaseId = i.CaseId
    WHERE i.InvoiceId = @InvoiceId;



    /* =========================================================
       Invoice Lines
       ========================================================= */

    SELECT
        il.InvoiceLineId,
        il.PublicId,

        il.InvoiceId,

        il.ServiceCode,
        il.Description,

        il.Units,
        il.Rate,
        il.Amount,

        il.SessionId,

        il.CreatedBy,
        il.CreatedDate,

        il.IsActive

    FROM [cases].[InvoiceLine] il
    WHERE il.InvoiceId = @InvoiceId
    ORDER BY il.InvoiceLineId ASC;



    /* =========================================================
       Audit Events
       ========================================================= */

    SELECT
        ai.AuditInvoiceId,
        ai.PublicId,

        ai.InvoiceId,
        ai.InvoiceLineId,

        ai.CaseId,
        ai.CaseNumber,

        ai.SessionId,

        ai.BillingRuleId,

        ai.AuditType,

        ai.AuditData,

        ai.CreatedBy,
        ai.CreatedDate

    FROM [cases].[AuditInvoice] ai
    WHERE ai.InvoiceId = @InvoiceId
    ORDER BY ai.CreatedDate ASC,
             ai.AuditInvoiceId ASC;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
EXEC [cases].[usp_Invoice_GenerateFromSession]
    @SessionId = 1,
    @CreatedBy = 'system';
*/
CREATE   PROCEDURE [cases].[usp_Invoice_GenerateFromSession]
(
    @SessionId     INT,
    @CreatedBy     NVARCHAR(100)
)
AS
BEGIN

    SET NOCOUNT ON;

    /* =========================================================
       Variables
       ========================================================= */

    DECLARE
        @CaseId                INT,
        @CaseNumber            NVARCHAR(50),

        @StartTime             DATETIME2(0),
        @EndTime               DATETIME2(0),

        @DurationMinutes       INT,

        @BillingRuleId         INT,
        @ServiceCode           NVARCHAR(20),
        @MinutesPerUnit        INT,
        @Rate                  DECIMAL(18,2),

        @Units                 DECIMAL(10,2),
        @Amount                DECIMAL(18,2),

        @InvoiceId             INT,
        @InvoiceLineId         INT,

        @InvoiceNumber         NVARCHAR(50),

        @AuditData             NVARCHAR(MAX);




    /* =========================================================
       Load Session
       ========================================================= */

    /*
        UPDATE THIS QUERY
        to match your actual session table structure.
    */

    SELECT
        @CaseId = s.CaseId,
        @StartTime = s.StartTime,
        @EndTime = s.EndTime
    FROM [cases].[Session] s
    WHERE s.SessionId = @SessionId;



    IF @CaseId IS NULL
    BEGIN
        THROW 50001, 'Session not found.', 1;
    END;



    /* =========================================================
       Load Case
       ========================================================= */

    SELECT
        @CaseNumber = c.CaseNumber
    FROM [cases].[Case] c
    WHERE c.CaseId = @CaseId;



    /* =========================================================
       Calculate Duration
       ========================================================= */

    SET @DurationMinutes =
        DATEDIFF(MINUTE, @StartTime, @EndTime);



    IF @DurationMinutes <= 0
    BEGIN
        THROW 50002, 'Invalid session duration.', 1;
    END;



    /* =========================================================
       Load Billing Rule
       MVP = first active rule
       ========================================================= */

    SELECT TOP 1
        @BillingRuleId = br.BillingRuleId,
        @ServiceCode = br.ServiceCode,
        @MinutesPerUnit = br.MinutesPerUnit,
        @Rate = br.Rate
    FROM [cases].[BillingRule] br
    WHERE br.IsActive = 1
    ORDER BY br.Priority ASC,
             br.BillingRuleId ASC;



    IF @BillingRuleId IS NULL
    BEGIN
        THROW 50003, 'No active billing rule found.', 1;
    END;



    /* =========================================================
       Calculate Units
       MVP Strategy = FLOOR
       ========================================================= */

    SET @Units =
        FLOOR(
            CAST(@DurationMinutes AS DECIMAL(10,2))
            / CAST(@MinutesPerUnit AS DECIMAL(10,2))
        );



    IF @Units <= 0
    BEGIN
        THROW 50004, 'Calculated units were zero.', 1;
    END;



    /* =========================================================
       Calculate Amount
       ========================================================= */

    SET @Amount =
        @Units * @Rate;



    /* =========================================================
       Generate Invoice Number
       ========================================================= */

    SET @InvoiceNumber =
        CONCAT(
            'INV-',
            FORMAT(GETDATE(), 'yyyyMMdd'),
            '-',
            RIGHT(
                '0000' +
                CAST(
                    ISNULL(
                    (
                        SELECT COUNT(*) + 1
                        FROM [cases].[Invoice]
                        WHERE CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE)
                    ),
                    1
                    ) AS VARCHAR(10)
                ),
                4
            )
        );



    /* =========================================================
       Create Invoice
       ========================================================= */

    INSERT INTO [cases].[Invoice]
    (
        InvoiceNumber,
        CaseId,
        CaseNumber,
        SessionId,
        InvoiceDate,
        Status,
        TotalAmount,
        CreatedBy
    )
    VALUES
    (
        @InvoiceNumber,
        @CaseId,
        @CaseNumber,
        @SessionId,
        SYSUTCDATETIME(),
        'Draft',
        @Amount,
        @CreatedBy
    );



    SET @InvoiceId = SCOPE_IDENTITY();



    /* =========================================================
       Create Invoice Line
       ========================================================= */

    INSERT INTO [cases].[InvoiceLine]
    (
        InvoiceId,
        ServiceCode,
        Description,
        Units,
        Rate,
        Amount,
        SessionId,
        CreatedBy
    )
    VALUES
    (
        @InvoiceId,
        @ServiceCode,
        CONCAT('Generated from session ', @SessionId),
        @Units,
        @Rate,
        @Amount,
        @SessionId,
        @CreatedBy
    );



    SET @InvoiceLineId = SCOPE_IDENTITY();



    /* =========================================================
       Build Audit JSON
       ========================================================= */

    SET @AuditData =
    (
        SELECT
            @InvoiceId             AS InvoiceId,
            @InvoiceNumber         AS InvoiceNumber,
            @CaseId                AS CaseId,
            @CaseNumber            AS CaseNumber,
            @SessionId             AS SessionId,
            @DurationMinutes       AS DurationMinutes,
            @BillingRuleId         AS BillingRuleId,
            @ServiceCode           AS ServiceCode,
            @MinutesPerUnit        AS MinutesPerUnit,
            @Units                 AS Units,
            @Rate                  AS Rate,
            @Amount                AS Amount,
            SYSUTCDATETIME()       AS GeneratedDate
        FOR JSON PATH,
        WITHOUT_ARRAY_WRAPPER
    );



    /* =========================================================
       Audit Row
       ========================================================= */

    INSERT INTO [cases].[AuditInvoice]
    (
        InvoiceId,
        InvoiceLineId,
        CaseId,
        CaseNumber,
        SessionId,
        BillingRuleId,
        AuditType,
        AuditData,
        CreatedBy
    )
    VALUES
    (
        @InvoiceId,
        @InvoiceLineId,
        @CaseId,
        @CaseNumber,
        @SessionId,
        @BillingRuleId,
        'InvoiceGenerated',
        @AuditData,
        @CreatedBy
    );



    /* =========================================================
       Return Invoice
       ========================================================= */

    SELECT
        i.InvoiceId,
        i.InvoiceNumber,
        i.CaseId,
        i.CaseNumber,
        i.SessionId,
        i.TotalAmount,
        i.Status,
        i.CreatedDate
    FROM [cases].[Invoice] i
    WHERE i.InvoiceId = @InvoiceId;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* 
EXEC [cases].[usp_Helper_Table_ReEnrich] @TableName = 'Session', @IndicatorColumn = 'SessionDate', @TopRows = 25; 
EXEC [cases].[usp_Helper_Table_ReEnrich] @TableName = 'Authorization', @IndicatorColumn = 'AuthId', @TopRows = 1; 
EXEC [cases].[usp_Helper_Table_ReEnrich] @TableName = 'Assessment', @IndicatorColumn = 'AssessmentDate', @TopRows = 1; 
*/

CREATE   PROCEDURE [cases].[usp_Helper_Table_ReEnrich]
(
    @TableName       SYSNAME,
    @IndicatorColumn SYSNAME,
    @TopRows         INT = 10
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id INT;
    DECLARE @JsonDocumentId INT;

    CREATE TABLE #Rows
    (
        Id INT,
        JsonDocumentId INT
    );

    DECLARE @sql NVARCHAR(MAX) =
    N'
        INSERT INTO #Rows (Id, JsonDocumentId)
        SELECT TOP (@TopRows)
               Id,
               JsonDocumentId
        FROM [cases].' + QUOTENAME(@TableName) + N'
        WHERE ' + QUOTENAME(@IndicatorColumn) + N' IS NULL
          AND JsonDocumentId IS NOT NULL
        ORDER BY Id;
    ';

    BEGIN TRY
        -- load rows safely
        EXEC sp_executesql
            @sql,
            N'@TopRows INT',
            @TopRows = @TopRows;

        DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT Id, JsonDocumentId
            FROM #Rows;

        OPEN cur;

        FETCH NEXT FROM cur INTO @Id, @JsonDocumentId;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                BEGIN TRAN;

                EXEC [cases].[usp_TableFieldMap_Apply]
                    @TableName = @TableName,
                    @RowId     = @Id,
                    @JsonDocId = @JsonDocumentId;

                COMMIT;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0
                    ROLLBACK;

                -- optional: log failure instead of stopping everything
                PRINT CONCAT(
                    'ReEnrich failed for Id=', @Id,
                    ' JsonDocumentId=', @JsonDocumentId,
                    ' Error=', ERROR_MESSAGE()
                );

                -- If you want FAIL FAST instead, uncomment:
                -- THROW;
            END CATCH;

            FETCH NEXT FROM cur INTO @Id, @JsonDocumentId;
        END

        CLOSE cur;
        DEALLOCATE cur;

    END TRY
    BEGIN CATCH
        -- outer failure (table query, cursor setup, etc.)
        IF OBJECT_ID('tempdb..#Rows') IS NOT NULL
            DROP TABLE #Rows;

        THROW;
    END CATCH
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- 2. usp_CreateClaim — logs ClaimCreated (single source: any future caller gets audited too)
CREATE PROCEDURE [cases].[usp_CreateClaim]
(
    @CaseId            INT,
    @SessionDocumentIds NVARCHAR(MAX),
    @ClaimId           INT OUTPUT,
    @ClaimNumber       VARCHAR(50) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;
		IF EXISTS
		(
			SELECT 1
			 FROM cases.ClaimSession cs
				JOIN cases.Claim cc  ON cc.ClaimId = cs.ClaimId 
				JOIN STRING_SPLIT(@SessionDocumentIds, ',') ss ON cs.SessionDocumentId = TRY_CAST(ss.value AS INT)
			WHERE cc.IsActive = 1
		)
		BEGIN
			THROW 50001, 'One or more sessions have already been assigned to a claim.', 1;
		END

        INSERT INTO cases.Claim
        (
            CaseId, Status, GeneratedDate, EdiDocumentId, IsActive, IsVoided
        )
        VALUES
        (
            @CaseId, 'Pending', SYSUTCDATETIME(), NULL, 1, 0
        );

        SET @ClaimId = SCOPE_IDENTITY();

        SET @ClaimNumber = CONCAT('CM', FORMAT(@ClaimId, '000000000'));

        UPDATE cases.Claim
        SET ClaimNumber = @ClaimNumber
        WHERE ClaimId = @ClaimId;

        INSERT INTO cases.ClaimSession
        (
            ClaimId, SessionDocumentId
        )
        SELECT
            @ClaimId, TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@SessionDocumentIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

        INSERT INTO cases.ClaimPipelineEvent (CaseId, ClaimId, EventType, Details)
        VALUES (@CaseId, @ClaimId, 'ClaimCreated', CONCAT('ClaimNumber=', @ClaimNumber, '; Sessions=', @SessionDocumentIds));

        COMMIT;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK;
        THROW;
    END CATCH
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Generic INSERT for any [cases] table that has CaseId, SourceDocumentId, JsonDocumentId.
-- After insert, calls usp_TableFieldMap_Apply to hydrate mapped columns from the JSON doc.
-- Replaces per-table copies like usp_Assessment_Resolve / usp_Authorization_Resolve.

-- exec [cases].[usp_CaseTable_Resolve] @TableName = 'Session', @DocId = 418, @CaseId = 5, @SrcDocId = 370

CREATE   PROCEDURE [cases].[usp_CaseTable_Resolve]
(
    @TableName SYSNAME,
    @DocId     INT,
    @CaseId    INT,
    @SrcDocId  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Whitelist: table must exist in [cases] and expose the three resolve columns
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'cases'
          AND TABLE_NAME   = @TableName
          AND COLUMN_NAME IN ('CaseId', 'SourceDocumentId', 'JsonDocumentId')
        GROUP BY TABLE_NAME
        HAVING COUNT(DISTINCT COLUMN_NAME) = 3
    )
        THROW 50002, 'Table is not a valid resolve target (missing CaseId/SourceDocumentId/JsonDocumentId).', 1;

    IF NOT EXISTS (SELECT 1 FROM [cases].[Document] WHERE DocumentId = @DocId AND IsActive = 1)
        THROW 50001, 'JSON document not found or inactive.', 1;

    DECLARE @tbl   NVARCHAR(258) = QUOTENAME(@TableName);
    DECLARE @NewId INT;

    -- Temp table captures the inserted Id across the sp_executesql boundary.
    -- SCOPE_IDENTITY() is unreliable inside sp_executesql called from a stored procedure.
    CREATE TABLE #InsertedId (Id INT);

    DECLARE @sql NVARCHAR(MAX) = N'
    INSERT INTO [cases].' + @tbl + N' (CaseId, JsonDocumentId, SourceDocumentId)
    OUTPUT INSERTED.Id INTO #InsertedId
    VALUES (@CaseId, @DocId, @SrcDocId);';

    EXEC sp_executesql @sql,
        N'@CaseId INT, @DocId INT, @SrcDocId INT',
        @CaseId = @CaseId, @DocId = @DocId, @SrcDocId = @SrcDocId;

    SELECT @NewId = Id FROM #InsertedId;
    DROP TABLE #InsertedId;

    -- Hydrate mapped columns from JSON doc
    IF @NewId IS NOT NULL
        EXEC [cases].[usp_TableFieldMap_Apply]
            @TableName = @TableName,
            @RowId     = @NewId,
            @JsonDocId = @DocId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- 
-- exec [cases].[usp_GetBillingContext]  @CaseId = 5
CREATE PROCEDURE [cases].[usp_GetBillingContext]
(
    @CaseId INT
)
AS
BEGIN
    SET NOCOUNT ON;
	SELECT
    ic.CaseId,
    ic.PayerId,
    p.PayerName,
    fs.FeeScheduleId,
    fsl.ProcedureCode,
    fsl.MinutesPerUnit,
    fsl.AllowedAmount
FROM cases.InsuranceCoverage ic
JOIN cases.Payer p
    ON p.PayerId = ic.PayerId
JOIN cases.FeeSchedule fs
    ON fs.PayerId = p.PayerId
JOIN cases.FeeScheduleLine fsl
    ON fsl.FeeScheduleId = fs.FeeScheduleId
WHERE ic.CaseId = @CaseId
  AND fs.IsActive = 1
  AND fsl.IsActive = 1;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- exec [cases].[usp_GetClaimInfo] @CaseId = 5 ,@Filter = 'Not Claimed';
-- exec [cases].[usp_GetClaimInfo] @ClaimId = 10;
CREATE PROCEDURE [cases].[usp_GetClaimInfo]
(
    @CaseId  INT = NULL,
    @ClaimId INT = NULL,
    @Filter  VARCHAR(50) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ClaimId IS NOT NULL AND @CaseId IS NULL
        SELECT @CaseId = CaseId FROM cases.Claim WHERE ClaimId = @ClaimId;

    ------------------------------------------------------------
    -- 1. Session / Claim Status
    ------------------------------------------------------------
    SELECT *
    FROM [cases].[vw_SessionClaimStatus]
    WHERE CaseId = @CaseId
      AND (@ClaimId IS NULL OR ClaimId = @ClaimId)
      AND
      (
          @Filter IS NULL
          OR @Filter = ''
          OR SessionClaimStatus = @Filter
          OR ClaimStatus = @Filter
          OR ClaimNumber = @Filter
      )
    ORDER BY SessionDate;

    ------------------------------------------------------------
    -- 2. Claim Information
    -- TODO
    ------------------------------------------------------------
    SELECT *
    FROM cases.Claim
    WHERE CaseId = @CaseId
      AND (@ClaimId IS NULL OR ClaimId = @ClaimId)
    ORDER BY status, [CreatedDate] DESC;

	------------------------------------------------------------
    -- 2b. Insurance Coverage
    ------------------------------------------------------------
    SELECT *
    FROM cases.InsuranceCoverage
    WHERE CaseId = @CaseId;


    ------------------------------------------------------------
    -- 3. Payer Information
    -- TODO
    ------------------------------------------------------------
    SELECT *
    FROM cases.Payer
    WHERE PayerCode='CIGNA'


    ------------------------------------------------------------
    -- 4. Patient Information
    -- TODO
    ------------------------------------------------------------
    SELECT *
    FROM cases.Patient
    WHERE CaseId = @CaseId


    ------------------------------------------------------------
    -- 5. Authorization Information
    -- TODO
    ------------------------------------------------------------
    SELECT *
    FROM [cases].[Authorization]
    WHERE CaseId = @CaseId -- -999 --@CaseId

    ------------------------------------------------------------
    -- 6. Provider Information
    -- TODO
    ------------------------------------------------------------
    SELECT *
    FROM cases.Provider
    WHERE [NPI] = '1234567893'

    ------------------------------------------------------------
    -- 7. Practice Configuration
    -- TODO
    ------------------------------------------------------------
    SELECT *
    FROM cases.PracticeConfiguration
    -- WHERE ...    
	
	------------------------------------------------------------
    -- 8. Fee Schedule
    ------------------------------------------------------------
    SELECT
        ic.CaseId,
        ic.PayerId,
        p.PayerName,
        fs.FeeScheduleId,
        fsl.ProcedureCode,
        fsl.MinutesPerUnit,
        fsl.AllowedAmount
    FROM cases.InsuranceCoverage ic
    JOIN cases.Payer p
        ON p.PayerId = ic.PayerId
    JOIN cases.FeeSchedule fs
        ON fs.PayerId = p.PayerId
    JOIN cases.FeeScheduleLine fsl
        ON fsl.FeeScheduleId = fs.FeeScheduleId
    WHERE ic.CaseId = @CaseId
      AND fs.IsActive = 1
      AND fsl.IsActive = 1;

END
GO
