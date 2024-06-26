/*==============================================================*/
/* Table: ADDRESS                                               */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'ADDRESS'
)
BEGIN
    CREATE TABLE dbo.ADDRESS
    (
        ADDRESS_ID INT IDENTITY(1, 1) NOT NULL,
        PARTICIPANT_ID INT NOT NULL,
        ADDRESS_TYPE VARCHAR(50) NULL,
        ADDRESS_LINE_1 VARCHAR(200) NULL,
        ADDRESS_LINE_2 VARCHAR(200) NULL,
        CITY VARCHAR(100) NULL,
        COUNTY VARCHAR(100) NULL,
        POST_CODE VARCHAR(50) NULL,
        LSOA VARCHAR(100) NULL,
        RECORD_START_DATE DATE NULL,
        RECORD_END_DATE DATE NULL,
        ACTIVE_FLAG CHAR NULL,
        LOAD_DATE DATE NOT NULL,
        CONSTRAINT PK_ADDRESS
            PRIMARY KEY (ADDRESS_ID)
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
/* Table: CONTACT_PREFERENCE                                    */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'CONTACT_PREFERENCE'
)
BEGIN
    CREATE TABLE dbo.CONTACT_PREFERENCE
    (
        CONTACT_PREFERENCE_ID INT IDENTITY(1, 1) NOT NULL,
        PARTICIPANT_ID INT NOT NULL,
        CONTACT_METHOD VARCHAR(100) NULL,
        PREFERRED_LANGUAGE VARCHAR(100) NULL,
        IS_INTERPRETER_REQUIRED CHAR NULL,
        TELEPHONE_NUMBER BIGINT NULL,
        MOBILE_NUMBER BIGINT NULL,
        EMAIL_ADDRESS VARCHAR(100) NULL,
        RECORD_START_DATE DATE NOT NULL,
        RECORD_END_DATE DATE NULL,
        ACTIVE_FLAG CHAR NULL,
        LOAD_DATE DATE NOT NULL,
        CONSTRAINT PK_CONTACT_PREFERENCE
            PRIMARY KEY (CONTACT_PREFERENCE_ID)
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
/* Table: PARTICIPANT                                           */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'PARTICIPANT'
)
BEGIN
    CREATE TABLE dbo.PARTICIPANT
    (
        PARTICIPANT_ID INT IDENTITY(1, 1) NOT NULL,
        COHORT_ID INT NOT NULL,
        GENDER_CD VARCHAR(2) NULL,
        NHS_NUMBER BIGINT NOT NULL,
        SUPERSEDED_BY_NHS_NUMBER BIGINT NULL,
        PARTICIPANT_BIRTH_DATE DATE NOT NULL,
        PARTICIPANT_DEATH_DATE DATE NULL,
        PARTICIPANT_PREFIX VARCHAR(20) NULL,
        PARTICIPANT_FIRST_NAME VARCHAR(100) NULL,
        PARTICIPANT_LAST_NAME VARCHAR(100) NULL,
        OTHER_NAME VARCHAR(100) NULL,
        PARTICIPANT_MARITAL_STATUS VARCHAR(100) NULL,
        PARTICIPANT_GENDER VARCHAR(2) NULL,
        PARTICIPANT_BIRTH_PLACE VARCHAR(100) NULL,
        PARTICIPANT_ETHNICITY VARCHAR(100) NULL,
        PARTICIPANT_RELIGION VARCHAR(100) NULL,
        PARTICIPANT_DECEASED VARCHAR(5) NULL,
        PARTICIPANT_REGISTERED_GP VARCHAR(200) NULL,
        GP_CONNECT VARCHAR(200) NULL,
        PRIMARY_CARE_PROVIDER VARCHAR(10) NULL,
        REASON_FOR_REMOVAL_CD VARCHAR(50) NULL,
        REMOVAL_DATE DATE NULL,
        RECORD_START_DATE DATE NULL,
        RECORD_END_DATE DATE NULL,
        ACTIVE_FLAG CHAR NOT NULL,
        LOAD_DATE DATE NULL,
        CONSTRAINT PK_PARTICIPANT
            PRIMARY KEY (PARTICIPANT_ID)
    );
END

/*==============================================================*/
/* Table: VALIDATION_EXCEPTION                                  */
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
        PARTICIPANT_ID INT IDENTITY(1, 1) not null,
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
        INTERPRETER_REQUIRED BIT NULL,
        INVALID_FLAG BIT NULL,
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
        CONSTRAINT PK_SCREENING_LKP
            PRIMARY KEY (SCREENING_ID)
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
        PARTICIPANT_ID BIGINT NOT NULL,
        SCREENING_ID BIGINT NULL,
        NHS_NUMBER BIGINT NULL,
        REASON_FOR_REMOVAL VARCHAR(50) NULL,
        REASON_FOR_REMOVAL_DT DATE NULL,
        BUSINESS_RULE_VERSION VARCHAR(50) NULL,
        EXCEPTION_FLAG CHAR(1) NULL,
        RECORD_INSERT_DATETIME DATE NULL,
        RECORD_UPDATE_DATETIME DATE NULL,
        CONSTRAINT PK_PARTICIPANT_MANAGEMENT
            PRIMARY KEY (PARTICIPANT_ID)
    );
END

/*==============================================================*/
/* Table: AGGREGATION_DATA Table                                */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'AGGREGATION_DATA'
)
BEGIN
    create table dbo.AGGREGATION_DATA
    (
        AGGREGATION_ID INT IDENTITY(1, 1) not null,
        COHORT_ID INT not null,
        GENDER_CD varchar(2) null,
        NHS_NUMBER BIGINT not null,
        SUPERSEDED_BY_NHS_NUMBER BIGINT null,
        PARTICIPANT_BIRTH_DATE DATE not null,
        PARTICIPANT_DEATH_DATE DATE null,
        PARTICIPANT_PREFIX VARCHAR(20) null,
        PARTICIPANT_FIRST_NAME VARCHAR(100) null,
        PARTICIPANT_LAST_NAME VARCHAR(100) null,
        OTHER_NAME VARCHAR(100) null,
        PARTICIPANT_MARITAL_STATUS VARCHAR(100) null,
        PARTICIPANT_GENDER VARCHAR(2) null,
        PARTICIPANT_BIRTH_PLACE VARCHAR(100) null,
        PARTICIPANT_ETHNICITY VARCHAR(100) null,
        PARTICIPANT_RELIGION VARCHAR(100) null,
        PARTICIPANT_DECEASED VARCHAR(5) null,
        PARTICIPANT_REGISTERED_GP VARCHAR(200) null,
        GP_CONNECT VARCHAR(200) null,
        PRIMARY_CARE_PROVIDER VARCHAR(10) null,
        REASON_FOR_REMOVAL_CD VARCHAR(50) null,
        REMOVAL_DATE DATE null,
        RECORD_START_DATE DATE null,
        RECORD_END_DATE DATE null,
        ACTIVE_FLAG CHAR not null,
        LOAD_DATE DATE null,
        EXTRACTED BIT not null
        constraint PK_AGGREGATION
            primary key (AGGREGATION_ID)
    );
END

/*==============================================================*/
/* Drop Table: SCREENING_PROGRAMS (If it exists)                */
/*==============================================================*/
IF OBJECT_ID('dbo.SCREENING_PROGRAMS') IS NOT NULL
BEGIN
    DROP TABLE dbo.SCREENING_PROGRAMS;
END

/*==============================================================*/
/* Alter Data Type: COHORT: PROGRAM_ID 							*/
/*   changed to BIGINT to match SCREENING_LKP 				*/
/*==============================================================*/
BEGIN
    ALTER Table dbo.COHORT ALTER COLUMN PROGRAM_ID BIGINT
END

/*==============================================================*/
/* Add Standard named constraints and relationships          */
/*==============================================================*/
IF NOT EXISTS
(
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_ADDRESS_PARTICIPANT'
          AND parent_object_id = OBJECT_ID('dbo.ADDRESS')
)
BEGIN
    ALTER TABLE dbo.ADDRESS
    ADD CONSTRAINT FK_ADDRESS_PARTICIPANT
        FOREIGN KEY (PARTICIPANT_ID)
        REFERENCES dbo.PARTICIPANT (PARTICIPANT_ID);
END;

IF NOT EXISTS
(
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_CONTACT_PARTICIPANT'
          AND parent_object_id = OBJECT_ID('dbo.CONTACT_PREFERENCE')
)
BEGIN
    ALTER TABLE dbo.CONTACT_PREFERENCE
    ADD CONSTRAINT FK_CONTACT_PARTICIPANT
        FOREIGN KEY (PARTICIPANT_ID)
        REFERENCES dbo.PARTICIPANT (PARTICIPANT_ID);
END;

IF NOT EXISTS
(
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_PARTICIPANT_COHORT'
          AND parent_object_id = OBJECT_ID('dbo.PARTICIPANT')
)
BEGIN
    ALTER TABLE dbo.PARTICIPANT
    ADD CONSTRAINT FK_PARTICIPANT_COHORT
        FOREIGN KEY (COHORT_ID)
        REFERENCES dbo.COHORT (COHORT_ID);
END;

IF NOT EXISTS
(
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_PARTICIPANT_GENDER_FK_GENDER_M'
          AND parent_object_id = OBJECT_ID('dbo.PARTICIPANT')
)
BEGIN
    ALTER TABLE dbo.PARTICIPANT
    ADD CONSTRAINT FK_PARTICIPANT_GENDER_FK_GENDER_M
        FOREIGN KEY (GENDER_CD)
        REFERENCES GENDER_MASTER (GENDER_CD);
END;

IF NOT EXISTS
(
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_COHORT_PROGRAM_FK_PROGRAM'
          AND parent_object_id = OBJECT_ID('dbo.COHORT')
)
BEGIN
    ALTER TABLE dbo.COHORT
    ADD CONSTRAINT FK_COHORT_PROGRAM_FK_PROGRAM
        FOREIGN KEY (PROGRAM_ID)
        REFERENCES SCREENING_LKP (SCREENING_ID);
END;

IF NOT EXISTS
(
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_PARTICIP_SCREENING_SCREENIN'
          AND parent_object_id = OBJECT_ID('dbo.PARTICIPANT_MANAGEMENT')
)
BEGIN
    ALTER TABLE dbo.PARTICIPANT_MANAGEMENT
    ADD CONSTRAINT FK_PARTICIP_SCREENING_SCREENIN
        FOREIGN KEY (SCREENING_ID)
        REFERENCES SCREENING_LKP (SCREENING_ID);
END;
