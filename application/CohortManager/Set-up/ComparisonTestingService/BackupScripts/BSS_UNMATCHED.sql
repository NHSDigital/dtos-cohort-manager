SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BSS_UNMATCHED](
	[nhs_number] [bigint] NULL,
	[date_of_birth] [varchar](max) NULL,
	[PRIMARY_CARE_PROVIDER] [varchar](max) NULL,
	[REASON_FOR_REMOVAL] [varchar](max) NULL,
	[REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE] [varchar](max) NULL,
	[date_of_death] [varchar](max) NULL,
	[postcode] [bigint] NULL,
	[GENDER] [varchar](max) NULL,
	[is_higher_risk] [bit] NULL,
	[age] [int] NULL,
	[DISCREPANCY_CATEGORY] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
