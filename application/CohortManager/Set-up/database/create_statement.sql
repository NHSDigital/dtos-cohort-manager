/*==============================================================*/
/* Table: BS_COHORT_DISTRIBUTION                                */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BS_COHORT_DISTRIBUTION'
)
BEGIN
    CREATE TABLE BS_COHORT_DISTRIBUTION (
        BS_COHORT_DISTRIBUTION_ID INT IDENTITY(1,1) NOT NULL,
        PARTICIPANT_ID BIGINT NULL,
        NHS_NUMBER BIGINT NULL,
        SUPERSEDED_NHS_NUMBER BIGINT NULL,
        PRIMARY_CARE_PROVIDER VARCHAR(10) NULL,
        PRIMARY_CARE_PROVIDER_FROM_DT DATE NULL,
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
        USUAL_ADDRESS_FROM_DT DATE NULL,
        CURRENT_POSTING VARCHAR(10) NULL,
        CURRENT_POSTING_FROM_DT DATE NULL,
        DATE_OF_DEATH DATE NULL,
        TELEPHONE_NUMBER_HOME VARCHAR(35) NULL,
        TELEPHONE_NUMBER_HOME_FROM_DT DATE NULL,
        TELEPHONE_NUMBER_MOB VARCHAR(35) NULL,
        TELEPHONE_NUMBER_MOB_FROM_DT DATE NULL,
        EMAIL_ADDRESS_HOME VARCHAR(100) NULL,
        EMAIL_ADDRESS_HOME_FROM_DT DATE NULL,
        PREFERRED_LANGUAGE VARCHAR(35) NULL,
        INTERPRETER_REQUIRED SMALLINT NULL,
        REASON_FOR_REMOVAL VARCHAR(10) NULL,
        REASON_FOR_REMOVAL_FROM_DT DATE NULL,
        IS_EXTRACTED SMALLINT NULL,
        RECORD_INSERT_DATETIME DATE NULL,
        RECORD_UPDATE_DATETIME DATE NULL,
        REQUEST_ID uniqueidentifier NULL,
        constraint BS_COHORT_DISTRIBUTION_PK
            primary key (BS_COHORT_DISTRIBUTION_ID)
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
        NAME_PREFIX VARCHAR(35) NULL,
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
        RECORD_TYPE VARCHAR(10) NULL,
        ELIGIBILITY_FLAG SMALLINT NULL,
        REASON_FOR_REMOVAL VARCHAR(10)  NULL,
        REASON_FOR_REMOVAL_FROM_DT DATE NULL,
        BUSINESS_RULE_VERSION VARCHAR (10)  NULL,
        EXCEPTION_FLAG SMALLINT  NULL,
        RECORD_INSERT_DATETIME DATETIME  NULL,
        RECORD_UPDATE_DATETIME DATETIME  NULL,
        CONSTRAINT PK_PARTICIPANT_MANAGEMENT
            PRIMARY KEY (PARTICIPANT_ID),
        CONSTRAINT FK_PARTICIP_SCREENING_SCREENIN
        FOREIGN KEY (SCREENING_ID)
        REFERENCES SCREENING_LKP (SCREENING_ID)
    );
END

/*==============================================================*/
/* Table: EXCEPTION_MANAGEMENT                                  */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'EXCEPTION_MANAGEMENT'
)
BEGIN
    CREATE TABLE [dbo].[EXCEPTION_MANAGEMENT]
    (
        FILE_NAME VARCHAR(250) NULL,
        NHS_NUMBER VARCHAR(50) NULL,
        DATE_CREATED DATE NULL,
        DATE_RESOLVED DATE NULL,
        RULE_ID INT NULL,
        RULE_DESCRIPTION VARCHAR(250) NULL,
        ERROR_RECORD VARCHAR(1500) NULL,
        CATEGORY INT NULL,
        SCREENING_NAME VARCHAR(100) NULL,
        EXCEPTION_DATE DATE NULL,
        COHORT_NAME VARCHAR(100) NULL,
        IS_FATAL SMALLINT NULL
    );
END

/*==============================================================*/
/* Table: BS_SELECT_GP_PRACTICE_LKP                             */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BS_SELECT_GP_PRACTICE_LKP'
)
BEGIN
CREATE TABLE [dbo].[BS_SELECT_GP_PRACTICE_LKP]
    (
      GP_PRACTICE_CODE VARCHAR(8),
      BSO VARCHAR(4),
      COUNTRY_CATEGORY VARCHAR(15),
      AUDIT_ID NUMERIC(38),
      AUDIT_CREATED_TIMESTAMP DATETIME,
      AUDIT_LAST_MODIFIED_TIMESTAMP DATETIME,
      AUDIT_TEXT VARCHAR(50)
    );
END

/*==============================================================*/
/* Table: BS_SELECT_OUTCODE_MAPPING_LKP                         */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BS_SELECT_OUTCODE_MAPPING_LKP'
)
BEGIN
CREATE TABLE [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP]
    (
      OUTCODE VARCHAR(4) PRIMARY KEY,
      BSO VARCHAR(4),
      AUDIT_ID NUMERIC(38),
      AUDIT_CREATED_TIMESTAMP DATETIME,
      AUDIT_LAST_MODIFIED_TIMESTAMP DATETIME,
      AUDIT_TEXT VARCHAR(50)
    );
END

/*==============================================================*/
/* Table: BS_SELECT_REQUEST_AUDIT Table                       */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BS_SELECT_REQUEST_AUDIT'
)
BEGIN
CREATE TABLE [dbo].[BS_SELECT_REQUEST_AUDIT]
    (
      REQUEST_ID UNIQUEIDENTIFIER NOT NULL,
      STATUS_CODE VARCHAR(3) NULL,
      CREATED_DATETIME DATETIME NULL,
    );
END

/*==============================================================*/
/* Table: GP_PRACTICES                                          */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'GP_PRACTICES'
)

BEGIN
CREATE TABLE GP_PRACTICES
(
    GP_PRACTICE_ID INT IDENTITY PRIMARY KEY,
    GP_PRACTICE_CODE varchar(8) NOT NULL,
    BSO_ORGANISATION_ID integer NOT NULL,
    OUTCODE varchar(4),
    GP_PRACTICE_GROUP_ID integer,
    TRANSACTION_ID integer NOT NULL,
    TRANSACTION_APP_DATE_TIME datetimeoffset NOT NULL,
    TRANSACTION_USER_ORG_ROLE_ID integer NOT NULL,
    TRANSACTION_DB_DATE_TIME datetimeoffset NOT NULL,
    GP_PRACTICE_NAME varchar(100),
    ADDRESS_LINE_1 varchar(35),
    ADDRESS_LINE_2 varchar(35),
    ADDRESS_LINE_3 varchar(35),
    ADDRESS_LINE_4 varchar(35),
    ADDRESS_LINE_5 varchar(35),
    POSTCODE varchar(8),
    TELEPHONE_NUMBER varchar(12),
    OPEN_DATE date,
    CLOSE_DATE date,
    FAILSAFE_DATE date,
    STATUS_CODE varchar(1) NOT NULL,
    LAST_UPDATED_DATE_TIME datetimeoffset NOT NULL,
    ACTIONED BIT NOT NULL DEFAULT 0,
    LAST_ACTIONED_BY_USER_ORG_ROLE_ID integer,
    LAST_ACTIONED_ON datetimeoffset
)
END

/*==============================================================*/
/* Table: EXCLUDED_SMU_LKP                                      */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'EXCLUDED_SMU_LKP'
)

BEGIN
CREATE TABLE EXCLUDED_SMU_LKP
(
    GP_PRACTICE_CODE VARCHAR(8) PRIMARY KEY
);
END

/*==============================================================*/
/* Table: CURRENT_POSTING_LKP                                   */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'CURRENT_POSTING_LKP'
)

CREATE TABLE CURRENT_POSTING_LKP
(
    POSTING VARCHAR(4) PRIMARY KEY,
    IN_USE VARCHAR(1),
    INCLUDED_IN_COHORT VARCHAR(1),
    POSTING_CATEGORY VARCHAR(10)
);

/*==============================================================*/
/* Table: LANGUAGE_CODES                                        */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'LANGUAGE_CODES'
)

CREATE TABLE LANGUAGE_CODES
(
    LANGUAGE_CODE VARCHAR(2) PRIMARY KEY,
    LANGUAGE_DESCRIPTION VARCHAR(100)
);

