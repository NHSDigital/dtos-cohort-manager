INSERT INTO [dbo].[SCREENING_LKP]
        ([SCREENING_NAME]
        ,[SCREENING_TYPE]
        ,[SCREENING_ACRONYM])
    VALUES
        ('Breast Screening'
        ,'Breast Screening Program'
        ,'BSS');

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

INSERT INTO [dbo].[BS_SELECT_GP_PRACTICE_LKP]
    VALUES

BULK INSERT [dbo].[BS_SELECT_GP_PRACTICE_LKP]
FROM 'Set-up\database\BS_SELECT_GP_PRACTICE_LKP_insertdata.csv'
WITH ( FORMAT = 'CSV');

BULK INSERT [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP]
FROM 'Set-up\database\BS_SELECT_OUTCODE_MAPPING_LKP_insertdata.csv'
WITH ( FORMAT = 'CSV');