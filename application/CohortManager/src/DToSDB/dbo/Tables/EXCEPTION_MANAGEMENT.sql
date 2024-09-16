CREATE TABLE [dbo].[EXCEPTION_MANAGEMENT] (
    [FILE_NAME]        VARCHAR (250)  NULL,
    [NHS_NUMBER]       BIGINT         NULL,
    [DATE_CREATED]     DATE           NULL,
    [DATE_RESOLVED]    DATE           NULL,
    [RULE_ID]          INT            NULL,
    [RULE_DESCRIPTION] VARCHAR (250)  NULL,
    [ERROR_RECORD]     VARCHAR (1500) NULL,
    [CATEGORY]         INT            NULL,
    [SCREENING_NAME]   VARCHAR (100)  NULL,
    [EXCEPTION_DATE]   DATE           NULL,
    [COHORT_NAME]      VARCHAR (100)  NULL,
    [IS_FATAL]         SMALLINT       NULL
);


GO

