SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[RUN_UNMATCHED_CAAS]
as
BEGIN
    insert into dbo.CAAS_UNMATCHED
    select 
        caas.*
    from dbo.CAAS_PARTICIPANT caas
    left join dbo.BSS_PARTICIPANT bss on 
        bss.nhs_number = caas.nhs_number
        AND bss.date_of_birth = caas.date_of_birth
    where bss.nhs_number  is NULL

END
GO
