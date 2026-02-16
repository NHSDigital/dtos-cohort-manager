namespace DataServices.Migrations.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class SqlDashboardViews : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // =====================================================
        // VIEW 1: vw_ExceptionManagement
        // Aggregated exception counts by Date, RuleId, Category, RuleDescription
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ExceptionManagement AS
                SELECT
                    CAST(DATE_CREATED AS DATE) AS DATE,
                    RULE_ID AS RULE_ID,
                    CATEGORY AS CATEGORY,
                    COUNT(*) AS NUMBER_OF_EXCEPTIONS,
                    RULE_DESCRIPTION AS RULE_DESCRIPTION
                FROM EXCEPTION_MANAGEMENT
                WHERE RULE_ID >= 0
                GROUP BY
                    CAST(DATE_CREATED AS DATE),
                    RULE_ID,
                    CATEGORY,
                    RULE_DESCRIPTION;
            ");

        // =====================================================
        // VIEW 2: vw_ServiceNowParticipants
        // Daily ServiceNow manual ADD counts split by High Risk / Standard
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ServiceNowParticipants AS
                SELECT
                    CAST(RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    IIF(IS_HIGHER_RISK = 1, 'High Risk Participant', 'Standard Risk Participant') AS CATEGORY,
                    COUNT(*) AS SERVICENOW_PARTICIPANTS
                FROM (
                    SELECT
                        RECORD_INSERT_DATETIME,
                        IS_HIGHER_RISK
                    FROM PARTICIPANT_MANAGEMENT
                    WHERE REFERRAL_FLAG = 1
                        AND RECORD_TYPE = 'ADD'
                ) AS DATE_SELECTION
                GROUP BY
                    CAST(RECORD_INSERT_DATETIME AS DATE),
                    IS_HIGHER_RISK;
            ");

        // =====================================================
        // VIEW 3: vw_ParticipantManagementRecords
        // Daily unique NHS number count from PARTICIPANT_MANAGEMENT
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ParticipantManagementRecords AS
                SELECT
                    DATE,
                    COUNT(*) AS PARTICIPANT_MANAGEMENT_RECORDS
                FROM (
                    SELECT DISTINCT
                        NHS_NUMBER,
                        CAST(COALESCE(RECORD_UPDATE_DATETIME, RECORD_INSERT_DATETIME) AS DATE) AS DATE
                    FROM PARTICIPANT_MANAGEMENT
                    WHERE RECORD_INSERT_DATETIME IS NOT NULL
                        OR RECORD_UPDATE_DATETIME IS NOT NULL
                ) AS UNIQUE_RECORDS
                GROUP BY DATE;
            ");

        // =====================================================
        // VIEW 4: vw_ParticipantDemographic
        // Daily unique NHS number count from PARTICIPANT_DEMOGRAPHIC (CaaS intake)
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ParticipantDemographic AS
                SELECT
                    DATE,
                    COUNT(*) AS PARTICIPANT_DEMOGRAPHIC_RECORDS
                FROM (
                    SELECT DISTINCT
                        NHS_NUMBER,
                        CAST(COALESCE(RECORD_UPDATE_DATETIME, RECORD_INSERT_DATETIME) AS DATE) AS DATE
                    FROM PARTICIPANT_DEMOGRAPHIC
                    WHERE RECORD_INSERT_DATETIME IS NOT NULL
                        OR RECORD_UPDATE_DATETIME IS NOT NULL
                ) AS UNIQUE_RECORDS
                GROUP BY DATE;
            ");

        // =====================================================
        // VIEW 5: vw_CohortDistribution
        // Consolidated: covers extraction counts AND superseded records
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_CohortDistribution AS
                SELECT
                    CAST(RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    IS_EXTRACTED AS IS_EXTRACTED,
                    IIF(SUPERSEDED_NHS_NUMBER IS NOT NULL, 1, 0) AS HAS_SUPERSEDED_NHS_NUMBER,
                    COUNT(DISTINCT NHS_NUMBER) AS RECORD_COUNT
                FROM BS_COHORT_DISTRIBUTION
                WHERE RECORD_INSERT_DATETIME IS NOT NULL
                GROUP BY
                    CAST(RECORD_INSERT_DATETIME AS DATE),
                    IS_EXTRACTED,
                    IIF(SUPERSEDED_NHS_NUMBER IS NOT NULL, 1, 0);
            ");

        // =====================================================
        // VIEW 6: vw_SupersededWithoutSupersedingRecord
        // Row-level: orphaned superseded records where no record
        // exists for the superseding NHS number
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_SupersededWithoutSupersedingRecord AS
                SELECT
                    cd1.NHS_NUMBER AS NHS_NUMBER,
                    cd1.SUPERSEDED_NHS_NUMBER AS SUPERSEDED_NHS_NUMBER,
                    cd1.RECORD_INSERT_DATETIME AS RECORD_INSERT_DATETIME,
                    cd1.IS_EXTRACTED AS IS_EXTRACTED
                FROM BS_COHORT_DISTRIBUTION cd1
                LEFT JOIN BS_COHORT_DISTRIBUTION cd2
                    ON cd1.SUPERSEDED_NHS_NUMBER = cd2.NHS_NUMBER
                WHERE cd1.SUPERSEDED_NHS_NUMBER IS NOT NULL
                    AND cd2.NHS_NUMBER IS NULL;
            ");

        // =====================================================
        // VIEW 7: vw_UnresolvedExceptions
        // Row-level: all unresolved exceptions with DaysOpen calc
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_UnresolvedExceptions AS
                SELECT
                    EXCEPTION_ID AS EXCEPTION_ID,
                    NHS_NUMBER AS NHS_NUMBER,
                    CAST(DATE_CREATED AS DATE) AS DATE_CREATED,
                    RULE_ID AS RULE_ID,
                    CATEGORY AS CATEGORY,
                    RULE_DESCRIPTION AS RULE_DESCRIPTION,
                    IS_FATAL AS IS_FATAL,
                    SERVICENOW_ID AS SERVICENOW_ID,
                    DATEDIFF(DAY, DATE_CREATED, GETDATE()) AS DAYS_OPEN
                FROM EXCEPTION_MANAGEMENT
                WHERE DATE_RESOLVED = '9999-12-31'
                    AND RULE_ID >= 0;
            ");

        // =====================================================
        // VIEW 8: vw_ParticipantRecordTypes
        // Consolidated: ADD/AMEND/DEL volume, exception counts,
        // and participant exception proportions
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ParticipantRecordTypes AS
                SELECT
                    CAST(RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    RECORD_TYPE AS RECORD_TYPE,
                    COUNT(*) AS RECORD_COUNT,
                    COUNT(DISTINCT NHS_NUMBER) AS UNIQUE_NHS_NUMBERS,
                    SUM(CASE WHEN EXCEPTION_FLAG = 1 THEN 1 ELSE 0 END) AS EXCEPTION_COUNT,
                    COUNT(DISTINCT CASE WHEN EXCEPTION_FLAG = 1 THEN NHS_NUMBER END) AS UNIQUE_NHS_NUMBERS_WITH_EXCEPTIONS
                FROM PARTICIPANT_MANAGEMENT
                WHERE RECORD_INSERT_DATETIME IS NOT NULL
                GROUP BY
                    CAST(RECORD_INSERT_DATETIME AS DATE),
                    RECORD_TYPE;
            ");

        // =====================================================
        // VIEW 9: vw_ServiceNowCasesDetailed
        // Detailed manual ADD breakdown with VHR referral reasons
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ServiceNowCasesDetailed AS
                SELECT
                    CAST(pm.RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    pm.NHS_NUMBER AS NHS_NUMBER,
                    IIF(pm.IS_HIGHER_RISK = 1, 'VHR', 'Non-VHR') AS RISK_CATEGORY,
                    pm.IS_HIGHER_RISK_ACTIVE AS IS_HIGHER_RISK_ACTIVE,
                    pm.HIGHER_RISK_REFERRAL_REASON_ID AS HIGHER_RISK_REFERRAL_REASON_ID,
                    hr.HIGHER_RISK_REFERRAL_REASON_CODE AS HIGHER_RISK_REFERRAL_REASON_CODE,
                    hr.HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION AS HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION,
                    pm.EXCEPTION_FLAG AS EXCEPTION_FLAG,
                    pm.ELIGIBILITY_FLAG AS ELIGIBILITY_FLAG
                FROM PARTICIPANT_MANAGEMENT pm
                LEFT JOIN HIGHER_RISK_REFERRAL_REASON_LKP hr
                    ON pm.HIGHER_RISK_REFERRAL_REASON_ID = hr.HIGHER_RISK_REFERRAL_REASON_ID
                WHERE pm.RECORD_TYPE = 'ADD'
                    AND pm.RECORD_INSERT_DATETIME IS NOT NULL;
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ServiceNowCasesDetailed;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ParticipantRecordTypes;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UnresolvedExceptions;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_SupersededWithoutSupersedingRecord;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CohortDistribution;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ParticipantDemographic;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ParticipantManagementRecords;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ServiceNowParticipants;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ExceptionManagement;");
    }
}
