/*==============================================================*/
/* Table: BS_COHORT_DISTRIBUTION Table                                */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BS_COHORT_DISTRIBUTION'
)
BEGIN
    CREATE TABLE BS_COHORT_DISTRIBUTION
    (
        PARTICIPANT_ID                BIGINT NOT NULL,
        NHS_NUMBER                    BIGINT NULL,
        SUPERSEDED_NHS_NUMBER         BIGINT NULL,
        PRIMARY_CARE_PROVIDER         VARCHAR(10) NULL,
        PRIMARY_CARE_PROVIDER_FROM_DT DATE NULL,
        NAME_PREFIX                   VARCHAR(10) NULL,
        GIVEN_NAME                    VARCHAR(100) NULL,
        OTHER_GIVEN_NAME              VARCHAR(100) NULL,
        FAMILY_NAME                   VARCHAR(100) NULL,
        PREVIOUS_FAMILY_NAME          VARCHAR(100) NULL,
        DATE_OF_BIRTH                 DATE NULL,
        GENDER                        SMALLINT NULL,
        ADDRESS_LINE_1                VARCHAR(100) NULL,
        ADDRESS_LINE_2                VARCHAR(100) NULL,
        ADDRESS_LINE_3                VARCHAR(100) NULL,
        ADDRESS_LINE_4                VARCHAR(100) NULL,
        ADDRESS_LINE_5                VARCHAR(100) NULL,
        POST_CODE                     VARCHAR(10) NULL,
        USUAL_ADDRESS_FROM_DT         DATE NULL,
        DATE_OF_DEATH                 DATE NULL,
        TELEPHONE_NUMBER_HOME         VARCHAR(35) NULL,
        TELEPHONE_NUMBER_HOME_FROM_DT DATE NULL,
        TELEPHONE_NUMBER_MOB          VARCHAR(35) NULL,
        TELEPHONE_NUMBER_MOB_FROM_DT  DATE NULL,
        EMAIL_ADDRESS_HOME            VARCHAR(100) NULL,
        EMAIL_ADDRESS_HOME_FROM_DT    DATE NULL,
        PREFERRED_LANGUAGE            VARCHAR(35) NULL,
        INTERPRETER_REQUIRED          SMALLINT NULL,
        REASON_FOR_REMOVAL            VARCHAR(50) NULL,
        REASON_FOR_REMOVAL_DT         DATE NULL,
        RECORD_INSERT_DATETIME        DATE NULL,
        RECORD_UPDATE_DATETIME        DATE NULL,
        IS_EXTRACTED                  BIT NOT NULL
        CONSTRAINT PK_BS_COHORT_DISTRIBUTION
          PRIMARY KEY (PARTICIPANT_ID)
    );
END

/*==============================================================*/
/* Table: COHORT                                                */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'COHORT'
)
BEGIN
    CREATE TABLE dbo.COHORT
    (
        COHORT_ID INT IDENTITY(1, 1) NOT NULL,
        PROGRAM_ID BIGINT NOT NULL,
        COHORT_NAME VARCHAR(100) NULL,
        COHORT_CREATE_DATE DATE NULL,
        LOAD_DATE DATE NULL,
        CONSTRAINT PK_COHORT
            PRIMARY KEY (COHORT_ID)
    );
END

/*==============================================================*/
/* Table: GENDER_MASTER                                         */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'GENDER_MASTER'
)
BEGIN
    CREATE TABLE dbo.GENDER_MASTER
    (
        GENDER_CD VARCHAR(2) NOT NULL,
        GENDER_DESC VARCHAR(10) NULL,
        CONSTRAINT PK_GENDER_MASTER
            PRIMARY KEY (GENDER_CD)
    );
END

/*==============================================================*/
/* Table: PARTICIPANT_DEMOGRAPHIC                               */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'PARTICIPANT_DEMOGRAPHIC'
)
BEGIN
    CREATE TABLE dbo.PARTICIPANT_DEMOGRAPHIC
    (
        PARTICIPANT_ID BIGINT IDENTITY(1, 1) not null,
        NHS_NUMBER BIGINT NULL,
        SUPERSEDED_BY_NHS_NUMBER BIGINT NULL,
        PRIMARY_CARE_PROVIDER VARCHAR(10) NULL,
        PRIMARY_CARE_PROVIDER_FROM_DT DATE NULL,
        CURRENT_POSTING VARCHAR(10) NULL,
        CURRENT_POSTING_FROM_DT DATE NULL,
        PREVIOUS_POSTING VARCHAR(10) NULL,
        PREV_POSTING_TO_DT DATE NULL,
        NAME_PREFIX VARCHAR(10) NULL,
        GIVEN_NAME VARCHAR(100) NULL,
        OTHER_GIVEN_NAME VARCHAR(100) NULL,
        FAMILY_NAME VARCHAR(100) NULL,
        PREVIOUS_FAMILY_NAME VARCHAR(100) NULL,
        DATE_OF_BIRTH DATE NULL,
        GENDER SMALLINT NULL,
        ADDRESS_LINE_1 VARCHAR(100) NULL,
        ADDRESS_LINE_2 VARCHAR(100) NULL,
        ADDRESS_LINE_3 VARCHAR(100) NULL,
        ADDRESS_LINE_4 VARCHAR(100) NULL,
        ADDRESS_LINE_5 VARCHAR(100) NULL,
        POST_CODE VARCHAR(10) NULL,
        PAF_KEY VARCHAR(10) NULL,
        USUAL_ADDRESS_FROM_DT DATE NULL,
        DATE_OF_DEATH DATE NULL,
        DEATH_STATUS SMALLINT NULL,
        TELEPHONE_NUMBER_HOME VARCHAR(35) NULL,
        TELEPHONE_NUMBER_HOME_FROM_DT DATE NULL,
        TELEPHONE_NUMBER_MOB VARCHAR(35) NULL,
        TELEPHONE_NUMBER_MOB_FROM_DT DATE NULL,
        EMAIL_ADDRESS_HOME VARCHAR(100) NULL,
        EMAIL_ADDRESS_HOME_FROM_DT DATE NULL,
        PREFERRED_LANGUAGE VARCHAR(35) NULL,
        INTERPRETER_REQUIRED SMALLINT NULL,
        INVALID_FLAG SMALLINT NULL,
        RECORD_INSERT_DATE_TIME DATE NULL,
        RECORD_UPDATE_DATE_TIME DATE NULL,
        constraint PK_PARTICIPANT_DEMOGRAPHIC
            primary key (PARTICIPANT_ID)
    );
END

/*==============================================================*/
/* Table: SCREENING_LKP                                         */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'SCREENING_LKP'
)
BEGIN
    CREATE TABLE SCREENING_LKP
    (
        SCREENING_ID BIGINT IDENTITY(1, 1) NOT NULL,
        SCREENING_NAME VARCHAR(50) NULL,
        SCREENING_TYPE VARCHAR(50) NULL,
        SCREENING_ACRONYM    VARCHAR(50) NULL,
        CONSTRAINT PK_SCREENING_LKP
            PRIMARY KEY (SCREENING_ID)
    );
END

/*==============================================================*/
/* Table: PARTICIPANT_MANAGEMENT                                */
/*==============================================================*/

IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
    AND TABLE_NAME = 'PARTICIPANT_MANAGEMENT'
)
BEGIN
    CREATE TABLE PARTICIPANT_MANAGEMENT
    (
        PARTICIPANT_ID BIGINT IDENTITY(1,1) NOT NULL,
        SCREENING_ID BIGINT NULL,
        NHS_NUMBER BIGINT NULL,
        REASON_FOR_REMOVAL VARCHAR(50) NULL,
        REASON_FOR_REMOVAL_DT DATE NULL,
        BUSINESS_RULE_VERSION VARCHAR(50) NULL,
        EXCEPTION_FLAG CHAR(1) NULL,
        RECORD_INSERT_DATETIME DATE NULL,
        RECORD_UPDATE_DATETIME DATE NULL,
        CONSTRAINT PK_PARTICIPANT_MANAGEMENT
            PRIMARY KEY (PARTICIPANT_ID),
        CONSTRAINT FK_PARTICIP_SCREENING_SCREENIN
        FOREIGN KEY (SCREENING_ID)
        REFERENCES SCREENING_LKP (SCREENING_ID)
    );
END

/*==============================================================*/
/* Table: VALIDATION_EXCEPTION Table                                */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'VALIDATION_EXCEPTION'
)
BEGIN
    CREATE TABLE [dbo].[VALIDATION_EXCEPTION]
    (
        VALIDATION_EXCEPTION_ID INT IDENTITY(1, 1) not null,
        NHS_NUMBER BIGINT NOT NULL,
        DATE_CREATED DATETIME NOT NULL,
        DATE_RESOLVED DATETIME NULL,
        RULE_ID INT NOT NULL,
        RULE_DESCRIPTION NVARCHAR(255) NOT NULL,
        RULE_CONTENT NVARCHAR(MAX) NOT NULL,
        CATEGORY INT NOT NULL,
        SCREENING_SERVICE INT NOT NULL,
        COHORT NVARCHAR(100) NOT NULL,
        FATAL BIT NOT NULL,
        CONSTRAINT PK_VALIDATION_EXCEPTION
            PRIMARY KEY (VALIDATION_EXCEPTION_ID)
    );
END
