SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[RUN_UNMATCHED_BSS]
as
BEGIN
    insert into dbo.BSS_UNMATCHED(
        [nhs_number]
      ,[date_of_birth]
      ,[PRIMARY_CARE_PROVIDER]
      ,[REASON_FOR_REMOVAL]
      ,[REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE]
      ,[date_of_death]
      ,[postcode]
      ,[GENDER]
      ,[is_higher_risk]
      ,[age]
      ,[DISCREPANCY_CATEGORY]
    )
    select 
        bss.[nhs_number]
        ,bss.[date_of_birth]
        ,bss.[PRIMARY_CARE_PROVIDER]
        ,bss.[REASON_FOR_REMOVAL]
        ,bss.[REASON_FOR_REMOVAL_BUSINESS_EFFECTIVE_FROM_DATE]
        ,bss.[date_of_death]
        ,bss.[postcode]
        ,bss.[GENDER]
        ,bss.[is_higher_risk]
        ,CASE 
            WHEN MONTH(GETDATE()) > MONTH(parse(bss.[date_of_birth] as date)) OR MONTH(GETDATE()) = MONTH(parse(bss.[date_of_birth] as date)) AND DAY(GETDATE()) >= DAY(parse(bss.[date_of_birth] as date)) 
                THEN DATEDIFF(year, parse(bss.[date_of_birth] as date), GETDATE()) 
            ELSE DATEDIFF(year, parse(bss.[date_of_birth] as date), GETDATE() ) - 1 
        END
        ,0
    from dbo.BSS_PARTICIPANT bss
    left join dbo.CAAS_PARTICIPANT caas on 
        bss.nhs_number = caas.nhs_number
        AND bss.date_of_birth = caas.date_of_birth
    where caas.nhs_number  is NULL
END
GO
