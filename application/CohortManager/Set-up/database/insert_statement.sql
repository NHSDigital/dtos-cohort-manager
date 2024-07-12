INSERT INTO [dbo].[SCREENING_LKP]
        ([SCREENING_NAME]
        ,[SCREENING_TYPE]
        ,[SCREENING_ACRONYM])
    VALUES
        ('Breast Screening'
        ,'Breast Screening Program'
        ,'BSS');

INSERT INTO [dbo].[COHORT]
        ([PROGRAM_ID]
        ,[COHORT_NAME]
        ,[COHORT_CREATE_DATE]
        ,[LOAD_DATE])
    VALUES
        (1
        ,'Cohort for Breast screening'
        ,'2024-03-27'
        ,'2024-03-27');

INSERT INTO [dbo].[COHORT]
        ([PROGRAM_ID]
        ,[COHORT_NAME]
        ,[COHORT_CREATE_DATE]
        ,[LOAD_DATE])
    VALUES
        (1
        ,'Cohort for Breast screening Research'
        ,'2024-03-27'
        ,'2024-03-27');

INSERT INTO [dbo].[GENDER_MASTER]
        ([GENDER_CD]
        ,[GENDER_DESC])
    VALUES
        ('1','Male');

INSERT INTO [dbo].[GENDER_MASTER]
        ([GENDER_CD]
        ,[GENDER_DESC])
    VALUES
        ('2','Female');
