/*==============================================================*/
/* Table: ADDRESS                                               */
/*==============================================================*/
create table dbo.ADDRESS (
    ADDRESS_ID           INT    IDENTITY(1, 1)   not null,
    PARTICIPANT_ID       INT                  not null,
    ADDRESS_TYPE         VARCHAR(50)          null,
    ADDRESS_LINE_1       VARCHAR(200)         null,
    ADDRESS_LINE_2       VARCHAR(200)         null,
    CITY                 VARCHAR(100)         null,
    COUNTY               VARCHAR(100)         null,
    POST_CODE            VARCHAR(50)          null,
    LSOA                 VARCHAR(100)         null,
    RECORD_START_DATE    DATE                 null,
    RECORD_END_DATE      DATE                 null,
    ACTIVE_FLAG          CHAR                 null,
    LOAD_DATE            DATE                 not null,
    constraint PK_ADDRESS primary key (ADDRESS_ID)
);

/*==============================================================*/
/* Table: COHORT                                                */
/*==============================================================*/
create table dbo.COHORT (
    COHORT_ID            INT    IDENTITY(1, 1)    not null,
    PROGRAM_ID           INT                  not null,
    COHORT_NAME          VARCHAR(100)         null,
    COHORT_CREATE_DATE   DATE                 null,
    LOAD_DATE            DATE                 null,
    constraint PK_COHORT primary key (COHORT_ID)
);

/*==============================================================*/
/* Table: CONTACT_PREFERENCE                                    */
/*==============================================================*/
create table dbo.CONTACT_PREFERENCE (
    CONTACT_PREFERENCE_ID INT     IDENTITY(1, 1)    not null,
    PARTICIPANT_ID       INT                  not null,
    CONTACT_METHOD       VARCHAR(100)         null,
    PREFERRED_LANGUAGE   VARCHAR(100)         null,
    IS_INTERPRETER_REQUIRED CHAR                 null,
    TELEPHONE_NUMBER     BIGINT                  null,
    MOBILE_NUMBER        BIGINT                  null,
    EMAIL_ADDRESS        VARCHAR(100)         null,
    RECORD_START_DATE    DATE                 not null,
    RECORD_END_DATE      DATE                 null,
    ACTIVE_FLAG          CHAR                 null,
    LOAD_DATE            DATE                 not null,
    constraint PK_CONTACT_PREFERENCE primary key (CONTACT_PREFERENCE_ID)
);

/*==============================================================*/
/* Table: GENDER_MASTER                                         */
/*==============================================================*/
create table dbo.GENDER_MASTER (
    GENDER_CD            varchar(2)           not null,
    GENDER_DESC          varchar(10)          null,
    constraint PK_GENDER_MASTER primary key (GENDER_CD)
);

/*==============================================================*/
/* Table: PARTICIPANT                                     */
/*==============================================================*/
create table dbo.PARTICIPANT (
    PARTICIPANT_ID       INT     IDENTITY(1, 1)      not null,
    COHORT_ID            INT                  not null,
    GENDER_CD            varchar(2)           null,
    NHS_NUMBER           BIGINT                  not null,
    SUPERSEDED_BY_NHS_NUMBER BIGINT                  null,
    PARTICIPANT_BIRTH_DATE DATE                 not null,
    PARTICIPANT_DEATH_DATE DATE                 null,
    PARTICIPANT_PREFIX   VARCHAR(20)          null,
    PARTICIPANT_FIRST_NAME VARCHAR(100)         null,
    PARTICIPANT_LAST_NAME VARCHAR(100)         null,
    OTHER_NAME           VARCHAR(100)         null,
    PARTICIPANT_MARITAL_STATUS VARCHAR(100)         null,
    PARTICIPANT_GENDER   VARCHAR(2)           null,
    PARTICIPANT_BIRTH_PLACE VARCHAR(100)         null,
    PARTICIPANT_ETHNICITY VARCHAR(100)         null,
    PARTICIPANT_RELIGION VARCHAR(100)         null,
    PARTICIPANT_DECEASED VARCHAR(5)           null,
    PARTICIPANT_REGISTERED_GP VARCHAR(200)         null,
    GP_CONNECT           VARCHAR(200)         null,
    PRIMARY_CARE_PROVIDER VARCHAR(10)          null,
    REASON_FOR_REMOVAL_CD VARCHAR(50)          null,
    REMOVAL_DATE         DATE                 null,
    RECORD_START_DATE    DATE                 null,
    RECORD_END_DATE      DATE                 null,
    ACTIVE_FLAG          CHAR                 not null,
    LOAD_DATE            DATE                 null,
    constraint PK_PARTICIPANT primary key (PARTICIPANT_ID)
);

/*==============================================================*/
/* Table: SCREENING_PROGRAMS                                    */
/*==============================================================*/
create table dbo.SCREENING_PROGRAMS (
    SCREENING_PROGRAM_ID INT    IDENTITY(1, 1)    not null,
    SCREENING_PROGRAM_NAME VARCHAR(50)          null,
    PROGRAM_DESC         VARCHAR(200)         null,
    constraint PK_SCREENING_PROGRAMS primary key (SCREENING_PROGRAM_ID)
);

/*==============================================================*/
/* Table: VALIDATION_EXCEPTION                                  */
/*==============================================================*/
create table dbo.VALIDATION_EXCEPTION (
    VALIDATION_EXCEPTION_ID INT IDENTITY(1,1) not null,
    RULE_ID INT not null,
    RULE_NAME NVARCHAR(200) not null,
    WORKFLOW NVARCHAR(50) not null,
    NHS_NUMBER BIGINT not null,
    DATE_CREATED DATETIME not null,
    constraint PK_VALIDATION_EXCEPTION primary key (VALIDATION_EXCEPTION_ID)
);



/*==============================================================*/
/* Add Standard named constraints and relationships          */
/*==============================================================*/

alter table dbo.ADDRESS
    add constraint FK_ADDRESS_PARTICIPANT foreign key (PARTICIPANT_ID)
        references dbo.PARTICIPANT (PARTICIPANT_ID);

alter table dbo.CONTACT_PREFERENCE
    add constraint FK_CONTACT_PARTICIPANT foreign key (PARTICIPANT_ID)
        references dbo.PARTICIPANT (PARTICIPANT_ID);

alter table dbo.PARTICIPANT
    add constraint FK_PARTICIPANT_COHORT foreign key (COHORT_ID)
        references dbo.COHORT (COHORT_ID);

alter table dbo.PARTICIPANT
    add constraint FK_PARTICIPANT_GENDER_FK_GENDER_M foreign key (GENDER_CD)
        references GENDER_MASTER (GENDER_CD);

alter table dbo.COHORT
    add constraint FK_COHORT_PROGRAM_FK_PROGRAM foreign key (PROGRAM_ID)
        references SCREENING_PROGRAMS (SCREENING_PROGRAM_ID);
