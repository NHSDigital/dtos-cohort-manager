CREATE TABLE [dbo].[BS_SELECT_OUTCODE_MAPPING_LKP] (
    [OUTCODE]                       VARCHAR (4)  NOT NULL,
    [BSO]                           VARCHAR (4)  NULL,
    [AUDIT_ID]                      NUMERIC (38) NULL,
    [AUDIT_CREATED_TIMESTAMP]       DATETIME     NULL,
    [AUDIT_LAST_MODIFIED_TIMESTAMP] DATETIME     NULL,
    [AUDIT_TEXT]                    VARCHAR (50) NULL,
    PRIMARY KEY CLUSTERED ([OUTCODE] ASC)
);


GO

