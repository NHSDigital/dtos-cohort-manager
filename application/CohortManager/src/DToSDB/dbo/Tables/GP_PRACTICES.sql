CREATE TABLE [dbo].[GP_PRACTICES] (
    [GP_PRACTICE_ID]                    INT                IDENTITY (1, 1) NOT NULL,
    [GP_PRACTICE_CODE]                  VARCHAR (8)        NOT NULL,
    [BSO_ORGANISATION_ID]               INT                NOT NULL,
    [OUTCODE]                           VARCHAR (4)        NULL,
    [GP_PRACTICE_GROUP_ID]              INT                NULL,
    [TRANSACTION_ID]                    INT                NOT NULL,
    [TRANSACTION_APP_DATE_TIME]         DATETIMEOFFSET (7) NOT NULL,
    [TRANSACTION_USER_ORG_ROLE_ID]      INT                NOT NULL,
    [TRANSACTION_DB_DATE_TIME]          DATETIMEOFFSET (7) NOT NULL,
    [GP_PRACTICE_NAME]                  VARCHAR (100)      NULL,
    [ADDRESS_LINE_1]                    VARCHAR (35)       NULL,
    [ADDRESS_LINE_2]                    VARCHAR (35)       NULL,
    [ADDRESS_LINE_3]                    VARCHAR (35)       NULL,
    [ADDRESS_LINE_4]                    VARCHAR (35)       NULL,
    [ADDRESS_LINE_5]                    VARCHAR (35)       NULL,
    [POSTCODE]                          VARCHAR (8)        NULL,
    [TELEPHONE_NUMBER]                  VARCHAR (12)       NULL,
    [OPEN_DATE]                         DATE               NULL,
    [CLOSE_DATE]                        DATE               NULL,
    [FAILSAFE_DATE]                     DATE               NULL,
    [STATUS_CODE]                       VARCHAR (1)        NOT NULL,
    [LAST_UPDATED_DATE_TIME]            DATETIMEOFFSET (7) NOT NULL,
    [ACTIONED]                          BIT                DEFAULT ((0)) NOT NULL,
    [LAST_ACTIONED_BY_USER_ORG_ROLE_ID] INT                NULL,
    [LAST_ACTIONED_ON]                  DATETIMEOFFSET (7) NULL,
    PRIMARY KEY CLUSTERED ([GP_PRACTICE_ID] ASC)
);


GO

