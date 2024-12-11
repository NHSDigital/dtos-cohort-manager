SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
alter PROCEDURE [dbo].[FLAG_DISCREPANCIES]
as
BEGIN

update dbo.BSS_UNMATCHED
set DISCREPANCY_CATEGORY = 
    case 
        when age < 44 and is_higher_risk = 1 then 1
        when age < 44 and is_higher_risk = 0 then 2
        when age > 74 then 3
        when (PRIMARY_CARE_PROVIDER is null OR PRIMARY_CARE_PROVIDER = '') 
            and (REASON_FOR_REMOVAL = 'DEATH' OR REASON_FOR_REMOVAL = 'DEA' or REASON_FOR_REMOVAL = 'UNCERTIFIED_DEATH') then 4
        when (PRIMARY_CARE_PROVIDER is null OR PRIMARY_CARE_PROVIDER = '') 
            and (REASON_FOR_REMOVAL != 'D' and REASON_FOR_REMOVAL != 'DEA' or REASON_FOR_REMOVAL is null) then 5
        when PRIMARY_CARE_PROVIDER like 'ZZZ%' then 6
        when GENDER ='MALE' then 7
        else 0
    end




END
GO
