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
        // Create the dashboard_reporting schema so we can assign
        // read-only permissions at the schema level.
        // =====================================================
        migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dashboard_reporting')
                BEGIN
                    EXEC('CREATE SCHEMA dashboard_reporting');
                END
            ");

        // =====================================================
        // Drop the previous dbo views created by the earlier
        // AddPowerBIViews migration – they are superseded by
        // the dashboard_reporting schema views below.
        // =====================================================
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_ServiceNowParticipants;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_ParticipantManagementRecords;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_ParticipantDemographic;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_ExceptionManagement;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_CohortDistribution;");

        // =====================================================
        // VIEW 1: vw_ExceptionManagement
        // Aggregated exception counts by Date, RuleId, Category, RuleDescription
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_ExceptionManagement AS
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
                CREATE OR ALTER VIEW dashboard_reporting.vw_ServiceNowParticipants AS
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
        // Daily unique participant count from PARTICIPANT_MANAGEMENT
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_ParticipantManagementRecords AS
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
        // Daily unique participant count from PARTICIPANT_DEMOGRAPHIC (CaaS intake)
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_ParticipantDemographic AS
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
                CREATE OR ALTER VIEW dashboard_reporting.vw_CohortDistribution AS
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
        // Aggregated: count of orphaned superseded records where
        // no record exists for the superseding NHS number,
        // grouped by date and extraction status (no PII returned)
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_SupersededWithoutSupersedingRecord AS
                SELECT
                    CAST(cd1.RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    cd1.IS_EXTRACTED AS IS_EXTRACTED,
                    COUNT(*) AS ORPHANED_SUPERSEDED_COUNT
                FROM BS_COHORT_DISTRIBUTION cd1
                LEFT JOIN BS_COHORT_DISTRIBUTION cd2
                    ON cd1.SUPERSEDED_NHS_NUMBER = cd2.NHS_NUMBER
                WHERE cd1.SUPERSEDED_NHS_NUMBER IS NOT NULL
                    AND cd2.NHS_NUMBER IS NULL
                GROUP BY
                    CAST(cd1.RECORD_INSERT_DATETIME AS DATE),
                    cd1.IS_EXTRACTED;
            ");

        // =====================================================
        // VIEW 7: vw_UnresolvedExceptions
        // Aggregated: unresolved exception counts by rule, category
        // and age bucket (no PII returned)
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_UnresolvedExceptions AS
                SELECT
                    CAST(DATE_CREATED AS DATE) AS DATE_CREATED,
                    RULE_ID AS RULE_ID,
                    CATEGORY AS CATEGORY,
                    RULE_DESCRIPTION AS RULE_DESCRIPTION,
                    IS_FATAL AS IS_FATAL,
                    COUNT(*) AS EXCEPTION_COUNT,
                    MIN(DATEDIFF(DAY, DATE_CREATED, GETDATE())) AS MIN_DAYS_OPEN,
                    MAX(DATEDIFF(DAY, DATE_CREATED, GETDATE())) AS MAX_DAYS_OPEN,
                    AVG(DATEDIFF(DAY, DATE_CREATED, GETDATE())) AS AVG_DAYS_OPEN
                FROM EXCEPTION_MANAGEMENT
                WHERE DATE_RESOLVED = '9999-12-31'
                    AND RULE_ID >= 0
                GROUP BY
                    CAST(DATE_CREATED AS DATE),
                    RULE_ID,
                    CATEGORY,
                    RULE_DESCRIPTION,
                    IS_FATAL;
            ");

        // =====================================================
        // VIEW 8: vw_ParticipantRecordTypes
        // Consolidated: ADD/AMEND/DEL volume, exception counts,
        // and participant exception proportions
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_ParticipantRecordTypes AS
                SELECT
                    CAST(RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    RECORD_TYPE AS RECORD_TYPE,
                    COUNT(*) AS RECORD_COUNT,
                    COUNT(DISTINCT NHS_NUMBER) AS UNIQUE_PARTICIPANT_COUNT,
                    SUM(CASE WHEN EXCEPTION_FLAG = 1 THEN 1 ELSE 0 END) AS EXCEPTION_COUNT,
                    COUNT(DISTINCT CASE WHEN EXCEPTION_FLAG = 1 THEN NHS_NUMBER END) AS UNIQUE_PARTICIPANTS_WITH_EXCEPTIONS
                FROM PARTICIPANT_MANAGEMENT
                WHERE RECORD_INSERT_DATETIME IS NOT NULL
                GROUP BY
                    CAST(RECORD_INSERT_DATETIME AS DATE),
                    RECORD_TYPE;
            ");

        // =====================================================
        // VIEW 9: vw_ServiceNowCasesDetailed
        // Aggregated manual ADD breakdown with VHR referral reasons
        // (no PII returned – counts replace row-level NHS numbers)
        // =====================================================
        migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW dashboard_reporting.vw_ServiceNowCasesDetailed AS
                SELECT
                    CAST(pm.RECORD_INSERT_DATETIME AS DATE) AS DATE,
                    IIF(pm.IS_HIGHER_RISK = 1, 'VHR', 'Non-VHR') AS RISK_CATEGORY,
                    pm.IS_HIGHER_RISK_ACTIVE AS IS_HIGHER_RISK_ACTIVE,
                    hr.HIGHER_RISK_REFERRAL_REASON_CODE AS HIGHER_RISK_REFERRAL_REASON_CODE,
                    hr.HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION AS HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION,
                    COUNT(*) AS RECORD_COUNT,
                    SUM(CASE WHEN pm.EXCEPTION_FLAG = 1 THEN 1 ELSE 0 END) AS EXCEPTION_COUNT,
                    SUM(CASE WHEN pm.ELIGIBILITY_FLAG = 1 THEN 1 ELSE 0 END) AS ELIGIBLE_COUNT
                FROM PARTICIPANT_MANAGEMENT pm
                LEFT JOIN HIGHER_RISK_REFERRAL_REASON_LKP hr
                    ON pm.HIGHER_RISK_REFERRAL_REASON_ID = hr.HIGHER_RISK_REFERRAL_REASON_ID
                WHERE pm.RECORD_TYPE = 'ADD'
                    AND pm.RECORD_INSERT_DATETIME IS NOT NULL
                GROUP BY
                    CAST(pm.RECORD_INSERT_DATETIME AS DATE),
                    IIF(pm.IS_HIGHER_RISK = 1, 'VHR', 'Non-VHR'),
                    pm.IS_HIGHER_RISK_ACTIVE,
                    hr.HIGHER_RISK_REFERRAL_REASON_CODE,
                    hr.HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION;
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_ServiceNowCasesDetailed;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_ParticipantRecordTypes;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_UnresolvedExceptions;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_SupersededWithoutSupersedingRecord;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_CohortDistribution;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_ParticipantDemographic;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_ParticipantManagementRecords;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_ServiceNowParticipants;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dashboard_reporting.vw_ExceptionManagement;");

        migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dashboard_reporting')
                BEGIN
                    EXEC('DROP SCHEMA dashboard_reporting');
                END
            ");
    }
}
