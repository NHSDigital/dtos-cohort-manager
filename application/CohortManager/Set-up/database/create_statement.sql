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
        RECORD_UPDATE_DATETIME DATE NULL
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
        REASON_FOR_REMOVAL VARCHAR(10) NULL,
        REASON_FOR_REMOVAL_DT DATE NULL,
        BUSINESS_RULE_VERSION VARCHAR(10) NULL,
        EXCEPTION_FLAG SMALLINT NULL,
        RECORD_INSERT_DATETIME DATETIME NULL,
        RECORD_UPDATE_DATETIME DATETIME NULL,
        CONSTRAINT PK_PARTICIPANT_MANAGEMENT
            PRIMARY KEY (PARTICIPANT_ID),
        CONSTRAINT FK_PARTICIP_SCREENING_SCREENIN
        FOREIGN KEY (SCREENING_ID)
        REFERENCES SCREENING_LKP (SCREENING_ID)
    );
END

/*==============================================================*/
/* Table: EXCEPTION_MANAGEMENT Table                                */
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
        NHS_NUMBER BIGINT NULL,
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
/* Table: BS_SELECT_GP_PRACTICE_LKP Table                       */
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
/* Table: BS_SELECT_OUTCODE_MAPPING_LKP Table                       */
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
      OUTCODE VARCHAR(4),
      BSO VARCHAR(4),
      AUDIT_ID NUMERIC(38),
      AUDIT_CREATED_TIMESTAMP DATETIME,
      AUDIT_LAST_MODIFIED_TIMESTAMP DATETIME,
      AUDIT_TEXT VARCHAR(50)
    );
END

/*==============================================================*/
/* Table: BSO_ORGANISATIONS Table                       */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'BSO_ORGANISATIONS'
)
BEGIN
    CREATE TABLE [dbo].[BSO_ORGANISATIONS]
    (
      BSO_ORGANISATION_ID INT IDENTITY(1,1) PRIMARY KEY,
      BSO_ORGANISATION_CODE VARCHAR(4) NOT NULL,
      BSO_ORGANISATION_NAME VARCHAR(60) NOT NULL,
      SAFETY_PERIOD NUMERIC(2) NOT NULL,
      RISP_RECALL_INTERVAL NUMERIC(2) NOT NULL,
      TRANSACTION_ID INTEGER NOT NULL,
      TRANSACTION_APP_DATE_TIME DATE NOT NULL,
      TRANSACTION_USER_ORG_ROLE_ID INTEGER NOT NULL,
      TRANSACTION_DB_DATE_TIME DATE NOT NULL,
      IGNORE_SELF_REFERRALS BOOLEAN NOT NULL,
      IGNORE_GP_REFERRALS BOOLEAN NOT NULL,
      IGNORE_EARLY_RECALL BOOLEAN NOT NULL,
      IS_ACTIVE BOOLEAN NOT NULL,
      LOWER_AGE_RANGE NUMERIC(2) NOT NULL,
      UPPER_AGE_RANGE NUMERIC(2) NOT NULL,
      LINK_CODE VARCHAR(10) NOT NULL,
      FOA_MAX_OFFSET NUMERIC(2) NOT NULL,
      BSO_RECALL_INTERVAL NUMERIC(2) NOT NULL,
      ADDRESS_LINE_1 VARCHAR(35),
      ADDRESS_LINE_2 VARCHAR(35),
      ADDRESS_LINE_3 VARCHAR(35),
      ADDRESS_LINE_4 VARCHAR(35),
      ADDRESS_LINE_5 VARCHAR(35),
      POSTCODE VARCHAR(8),
      TELEPHONE_NUMBER VARCHAR(18),
      EXTENSION VARCHAR(6),
      FAX_NUMBER VARCHAR(18),
      EMAIL_ADDRESS VARCHAR(100),
      OUTGOING_TRANSFER_NUMBER NUMERIC(6) NOT NULL,
      INVITE_LIST_SEQUENCE_NUMBER NUMERIC(8) NOT NULL,
      FAILSAFE_DATE_OF_MONTH NUMERIC(2) NOT NULL,
      FAILSAFE_MONTHS NUMERIC(1) NOT NULL,
      FAILSAFE_MIN_AGE_YEARS NUMERIC(2) NOT NULL,
      FAILSAFE_MIN_AGE_MONTHS NUMERIC(2) NOT NULL,
      FAILSAFE_MAX_AGE_YEARS NUMERIC(2) NOT NULL,
      FAILSAFE_MAX_AGE_MONTHS NUMERIC(2) NOT NULL,
      FAILSAFE_LAST_RUN DATE NOT NULL,
      IS_AGEX BOOLEAN NOT NULL,
      IS_AGEX_ACTIVE BOOLEAN NOT NULL,
      AUTO_BATCH_LAST_RUN DATE,
      AUTO_BATCH_MAX_DATE_TIME_PROCESSED DATE,
      BSO_REGION_ID INTEGER,
      ADMIN_EMAIL_ADDRESS VARCHAR(100),
      IEP_DETAILS TEXT,
      NOTES TEXT,
      RLP_DATE_ENABLED DATE
    );
END

-- Add description to the table
EXEC sp_addextendedproperty
    @name = N'Description',
    @value = N'Contains BSO level details/parameters. One row per BSO. Changes are timestamped and trigger updates to audit_bso_organisations.',
    @level0type = N'Schema', @level0name = dbo,
    @level1type = N'Table', @level1name = BSO_ORGANISATIONS;

-- Add description to columns
EXEC sp_addextendedproperty
    @name = N'Description',
    @value = N'The date and time that the auto batch process last finished processing for the BSO. This is used to ensure only a single app server processes the data per run',
    @level0type = N'Schema', @level0name = dbo,
    @level1type = N'Table', @level1name = BSO_ORGANISATIONS,
    @level2type = N'Column', @level2name = AUTO_BATCH_LAST_RUN;

EXEC sp_addextendedproperty
    @name = N'Description',
    @value = N'The date and time used to determine the inclusive upper limit of the data that was included in the last auto batch run',
    @level0type = N'Schema', @level0name = dbo,
    @level1type = N'Table', @level1name = BSO_ORGANISATIONS,
    @level2type = N'Column', @level2name = AUTO_BATCH_MAX_DATE_TIME_PROCESSED;

EXEC sp_addextendedproperty
    @name = N'Description',
    @value = N'Cannot be false if is_agex_active is true',
    @level0type = N'Schema', @level0name = dbo,
    @level1type = N'Table', @level1name = BSO_ORGANISATIONS,
    @level2type = N'Column', @level2name = IS_AGEX;
