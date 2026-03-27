using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerBIViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW vw_ServiceNowParticipants AS
                SELECT
                    CAST(RECORD_INSERT_DATETIME AS DATE) AS Date,
                    IIF(IS_HIGHER_RISK = 1, 'High Risk Participant', 'Standard Risk Participant') AS Category,
                    COUNT(*) AS ServiceNow_Participants
                FROM (
                    SELECT
                        RECORD_INSERT_DATETIME,
                        IS_HIGHER_RISK
                    FROM PARTICIPANT_MANAGEMENT
                    WHERE REFERRAL_FLAG = 1
                        AND RECORD_TYPE = 'ADD'
                ) AS DateSelection
                GROUP BY
                    CAST(RECORD_INSERT_DATETIME AS DATE),
                    IS_HIGHER_RISK;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_ParticipantManagementRecords AS
                SELECT
                    Date,
                    COUNT(*) AS Participant_Management_Records
                FROM (
                    SELECT DISTINCT
                        NHS_NUMBER,
                        CAST(COALESCE(RECORD_UPDATE_DATETIME, RECORD_INSERT_DATETIME) AS DATE) AS Date
                    FROM PARTICIPANT_MANAGEMENT
                    WHERE RECORD_INSERT_DATETIME IS NOT NULL
                        OR RECORD_UPDATE_DATETIME IS NOT NULL
                ) unique_records
                GROUP BY Date;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_ParticipantDemographic AS
                SELECT
                    Date,
                    COUNT(*) AS Participant_Demographic_Records
                FROM (
                    SELECT DISTINCT
                        NHS_NUMBER,
                        CAST(COALESCE(RECORD_UPDATE_DATETIME, RECORD_INSERT_DATETIME) AS DATE) AS Date
                    FROM PARTICIPANT_DEMOGRAPHIC
                    WHERE RECORD_INSERT_DATETIME IS NOT NULL
                        OR RECORD_UPDATE_DATETIME IS NOT NULL
                ) unique_records
                GROUP BY Date;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_ExceptionManagement AS
                SELECT
                    CAST(DATE_CREATED AS DATE) AS Date,
                    RULE_ID AS RuleId,
                    CATEGORY AS Category,
                    COUNT(*) AS Number_Of_Exceptions,
                    NULLIF(MAX(DATE_RESOLVED), '9999-12-31') AS DATE_RESOLVED,
                    RULE_DESCRIPTION AS RULE_DESCRIPTION
                FROM EXCEPTION_MANAGEMENT
                WHERE RULE_ID >= 0
                GROUP BY
                    CAST(DATE_CREATED AS DATE),
                    RULE_ID,
                    CATEGORY,
                    RULE_DESCRIPTION;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_CohortDistribution AS
                SELECT
                    Date,
                    COUNT(DISTINCT NHS_NUMBER) AS Cohort_Distribution_Records
                FROM (
                    SELECT DISTINCT
                        NHS_NUMBER,
                        CAST(COALESCE(RECORD_UPDATE_DATETIME, RECORD_INSERT_DATETIME) AS DATE) AS Date
                    FROM BS_COHORT_DISTRIBUTION
                    WHERE RECORD_INSERT_DATETIME IS NOT NULL
                ) unique_records
                GROUP BY Date;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ServiceNowParticipants;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ParticipantManagementRecords;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ParticipantDemographic;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ExceptionManagement;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CohortDistribution;");
        }
    }
}
