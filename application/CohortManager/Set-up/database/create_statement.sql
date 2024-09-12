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
CREATE TABLE bso_organisations
(
  bso_organisation_id         SERIAL PRIMARY KEY,
  bso_organisation_code         VARCHAR(4) NOT NULL,
  bso_organisation_name         VARCHAR(60) NOT NULL,
  safety_period             NUMERIC(2) NOT NULL,
  risp_recall_interval          NUMERIC(2) NOT NULL,
  transaction_id            INTEGER NOT NULL,
  transaction_app_date_time       TIMESTAMP WITH TIME NOT NULL,
  transaction_user_org_role_id      INTEGER NOT NULL,
  transaction_db_date_time        TIMESTAMP WITH TIME ZONE NOT NULL,
  ignore_self_referrals         boolean NOT NULL,
  ignore_gp_referrals         boolean NOT NULL,
  ignore_early_recall         boolean NOT NULL,
  is_active               boolean NOT NULL,
  lower_age_range           NUMERIC(2) NOT NULL,
  upper_age_range           NUMERIC(2) NOT NULL,
  link_code               VARCHAR(10) NOT NULL,
  foa_max_offset            NUMERIC(2) NOT NULL,
  bso_recall_interval         NUMERIC(2) NOT NULL,
  address_line_1            VARCHAR(35),
  address_line_2            VARCHAR(35),
  address_line_3            VARCHAR(35),
  address_line_4            VARCHAR(35),
  address_line_5            VARCHAR(35),
  postcode                VARCHAR(8),
  telephone_number            VARCHAR(18),
  extension               VARCHAR(6),
  fax_number              VARCHAR(18),
  email_address             VARCHAR(100),
  outgoing_transfer_number        NUMERIC(6) NOT NULL,
  invite_list_sequence_number     NUMERIC(8) NOT NULL,
  failsafe_date_of_month        NUMERIC(2) NOT NULL,
  failsafe_months           NUMERIC(1) NOT NULL,
  failsafe_min_age_years        NUMERIC(2) NOT NULL,
  failsafe_min_age_months       NUMERIC(2) NOT NULL,
  failsafe_max_age_years        NUMERIC(2) NOT NULL,
  failsafe_max_age_months       NUMERIC(2) NOT NULL,
  failsafe_last_run           DATE NOT NULL,
  is_agex                           boolean NOT NULL,
  is_agex_active                      boolean NOT NULL,
  auto_batch_last_run         TIMESTAMP WITH TIME ZONE,
  auto_batch_max_date_time_processed  TIMESTAMP WITH TIME ZONE,
  bso_region_id             INTEGER,
  admin_email_address         VARCHAR(100),
  iep_details               text,
  notes                 text,
  rlp_date_enabled            DATE
);

COMMENT ON TABLE bso_organisations IS 'Contains BSO level details/parameters. One row per BSO. Changes are timestamped and trigger updates to audit_bso_organisations.';
COMMENT ON COLUMN bso_organisations.auto_batch_last_run IS 'The date and time that the auto batch process last finished processing for the BSO. This is used to ensure only a single app server processes the data per run';
COMMENT ON COLUMN bso_organisations.auto_batch_max_date_time_processed IS 'The date and time used to determine the inclusive upper limit of the data that was included in the last auto batch run';
COMMENT ON COLUMN bso_organisations.is_agex IS 'Cannot be false if is_agex_active is true';
