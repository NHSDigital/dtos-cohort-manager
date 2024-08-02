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
