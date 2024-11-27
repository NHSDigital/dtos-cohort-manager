IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BSS_PARTICIPANT'
)
BEGIN
    CREATE TABLE [dbo].[CAAS_PARTICIPANT] 
    (
        [Record_Type] VARCHAR (10) NULL,
        [Change_Time_Stamp] NUMERIC (20) NULL,
        [Serial_Change_Number] INT NULL,
        [NHS_Number] VARCHAR (10) NULL,
        [Superseded_By_NHS_Number] VARCHAR (10) NULL,
        [Primary_Care_Provider] VARCHAR (10) NULL,
        [Primary_Care_Provider_Business_Effective_From_Date] DATE NULL,
        [Current_Posting] VARCHAR (3) NULL,
        [Current_Posting_Business_Effective_From_Date] DATE NULL,
        [Name_Prefix] VARCHAR (35) NULL,
        [Given_Name] VARCHAR (35) NULL,
        [Other_Given_Name(s)] VARCHAR (100) NULL,
        [Family_Name] VARCHAR (35) NULL,
        [Previous_Family_Name] VARCHAR (35) NULL,
        [Date_Of_Birth] DATE NULL,
        [Gender] TINYINT NULL,
        [Address_Line_1] VARCHAR (35) NULL,
        [Address_Line_2] VARCHAR (35) NULL,
        [Address_Line_3] VARCHAR (35) NULL,
        [Address_Line_4] VARCHAR (35) NULL,
        [Address_Line_5] VARCHAR (35) NULL,
        [Postcode] VARCHAR (8) NULL,
        [PAF_key] VARCHAR (8) NULL,
        [Usual_Address_Business_Effective_From_Date] DATE NULL,
        [Reason_For_Removal] VARCHAR (3) NULL,
        [Reason_For_Removal_Business_Effective_From_Date] DATE NULL,
        [Date_Of_Death(Formal)] DATE NULL,
        [Death_Status] TINYINT NULL,
        [Telephone_Number(Home)] VARCHAR (32) NULL,
        [Telephone_Number(Home)_Business_Effective_From_Date] DATE NULL,
        [Telephone_Number(Mobile)] VARCHAR (32) NULL,
        [Telephone_Number(Mobile)_Business_Effective_From_Date] DATE NULL,
        [E-mail_Address(Home)] NVARCHAR (90) NULL,
        [E-mail_Address(Home)_Business_Effective_From_Date] DATE NULL,
        [Preferred_Language] VARCHAR (2) NULL,
        [Interpreter_Required] BIT NULL,
        [Invalid_Flag] BIT NULL,
        [Eligibility] BIT NULL
    );
END

IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BSS_PARTICIPANT'
)
BEGIN
    CREATE TABLE [dbo].[BSS_PARTICIPANT]
    (
        NHS_NUMBER VARCHAR(10) NOT NULL,
        DATE_OF_BIRTH DATE NOT NULL,
        GP_PRACTICE_CODE VARCHAR(8),
        REASON_FOR_REMOVAL VARCHAR(100),
        REASON_FOR_REMOVAL_FROM_DT DATE,
        DATE_OF_DEATH DATE,
        POSTCODE VARCHAR(8),
        GENDER_CD VARCHAR(20),
        IS_HIGHER_RISK TINYINT NOT NULL
    );
END
