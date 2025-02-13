namespace DataServices.Database.Migrations;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable



    /// <inheritdoc />
    public partial class SetUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "BS_COHORT_DISTRIBUTION",
                schema: "dbo",
                columns: table => new
                {
                    BS_COHORT_DISTRIBUTION_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PARTICIPANT_ID = table.Column<long>(type: "bigint", nullable: false),
                    NHS_NUMBER = table.Column<long>(type: "bigint", nullable: false),
                    SUPERSEDED_NHS_NUMBER = table.Column<long>(type: "bigint", nullable: false),
                    PRIMARY_CARE_PROVIDER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PRIMARY_CARE_PROVIDER_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NAME_PREFIX = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    GIVEN_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OTHER_GIVEN_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FAMILY_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PREVIOUS_FAMILY_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DATE_OF_BIRTH = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GENDER = table.Column<short>(type: "smallint", nullable: false),
                    ADDRESS_LINE_1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_4 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_5 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    POST_CODE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    USUAL_ADDRESS_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CURRENT_POSTING = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CURRENT_POSTING_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DATE_OF_DEATH = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TELEPHONE_NUMBER_HOME = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    TELEPHONE_NUMBER_HOME_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TELEPHONE_NUMBER_MOB = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    TELEPHONE_NUMBER_MOB_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EMAIL_ADDRESS_HOME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EMAIL_ADDRESS_HOME_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PREFERRED_LANGUAGE = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    INTERPRETER_REQUIRED = table.Column<short>(type: "smallint", nullable: false),
                    REASON_FOR_REMOVAL = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    REASON_FOR_REMOVAL_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IS_EXTRACTED = table.Column<short>(type: "smallint", nullable: false),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    REQUEST_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BS_COHORT_DISTRIBUTION", x => x.BS_COHORT_DISTRIBUTION_ID);
                });

            migrationBuilder.CreateTable(
                name: "BS_SELECT_GP_PRACTICE_LKP",
                schema: "dbo",
                columns: table => new
                {
                    GP_PRACTICE_CODE = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BSO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    COUNTRY_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AUDIT_ID = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AUDIT_CREATED_TIMESTAMP = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AUDIT_LAST_MODIFIED_TIMESTAMP = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AUDIT_TEXT = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BS_SELECT_GP_PRACTICE_LKP", x => x.GP_PRACTICE_CODE);
                });

            migrationBuilder.CreateTable(
                name: "BS_SELECT_OUTCODE_MAPPING_LKP",
                schema: "dbo",
                columns: table => new
                {
                    OUTCODE = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BSO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AUDIT_ID = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AUDIT_CREATED_TIMESTAMP = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AUDIT_LAST_MODIFIED_TIMESTAMP = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AUDIT_TEXT = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BS_SELECT_OUTCODE_MAPPING_LKP", x => x.OUTCODE);
                });

            migrationBuilder.CreateTable(
                name: "CURRENT_POSTING_LKP",
                schema: "dbo",
                columns: table => new
                {
                    POSTING = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IN_USE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    INCLUDED_IN_COHORT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    POSTING_CATEGORY = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CURRENT_POSTING_LKP", x => x.POSTING);
                });

            migrationBuilder.CreateTable(
                name: "EXCEPTION_MANAGEMENT",
                schema: "dbo",
                columns: table => new
                {
                    EXCEPTION_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FILE_NAME = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NHS_NUMBER = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DATE_CREATED = table.Column<DateTime>(type: "datetime", nullable: true),
                    DATE_RESOLVED = table.Column<DateTime>(type: "datetime", nullable: true),
                    RULE_ID = table.Column<int>(type: "int", nullable: true),
                    RULE_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ERROR_RECORD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CATEGORY = table.Column<int>(type: "int", nullable: true),
                    SCREENING_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EXCEPTION_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    COHORT_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IS_FATAL = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EXCEPTION_MANAGEMENT", x => x.EXCEPTION_ID);
                });

            migrationBuilder.CreateTable(
                name: "EXCLUDED_SMU_LKP",
                schema: "dbo",
                columns: table => new
                {
                    GP_PRACTICE_CODE = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EXCLUDED_SMU_LKP", x => x.GP_PRACTICE_CODE);
                });

            migrationBuilder.CreateTable(
                name: "GENE_CODE_LKP",
                schema: "dbo",
                columns: table => new
                {
                    GENE_CODE_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GENE_CODE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GENE_CODE_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GENE_CODE_LKP", x => x.GENE_CODE_ID);
                });

            migrationBuilder.CreateTable(
                name: "GP_PRACTICES",
                schema: "dbo",
                columns: table => new
                {
                    GP_PRACTICE_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GP_PRACTICE_CODE = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    BSO_ORGANISATION_ID = table.Column<int>(type: "int", nullable: false),
                    OUTCODE = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    GP_PRACTICE_GROUP_ID = table.Column<int>(type: "int", nullable: true),
                    TRANSACTION_ID = table.Column<int>(type: "int", nullable: false),
                    TRANSACTION_APP_DATE_TIME = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TRANSACTION_USER_ORG_ROLE_ID = table.Column<int>(type: "int", nullable: false),
                    TRANSACTION_DB_DATE_TIME = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GP_PRACTICE_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_1 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_2 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_3 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_4 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_5 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    POSTCODE = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    TELEPHONE_NUMBER = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    OPEN_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CLOSE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FAILSAFE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    STATUS_CODE = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    LAST_UPDATED_DATE_TIME = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ACTIONED = table.Column<bool>(type: "bit", nullable: false),
                    LAST_ACTIONED_BY_USER_ORG_ROLE_ID = table.Column<int>(type: "int", nullable: true),
                    LAST_ACTIONED_ON = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GP_PRACTICES", x => x.GP_PRACTICE_ID);
                });

            migrationBuilder.CreateTable(
                name: "HIGHER_RISK_REFERRAL_REASON_LKP",
                schema: "dbo",
                columns: table => new
                {
                    HIGHER_RISK_REFERRAL_REASON_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HIGHER_RISK_REFERRAL_REASON_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HIGHER_RISK_REFERRAL_REASON_LKP", x => x.HIGHER_RISK_REFERRAL_REASON_ID);
                });

            migrationBuilder.CreateTable(
                name: "LANGUAGE_CODES",
                schema: "dbo",
                columns: table => new
                {
                    LANGUAGE_CODE = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LANGUAGE_DESCRIPTION = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LANGUAGE_CODES", x => x.LANGUAGE_CODE);
                });

            migrationBuilder.CreateTable(
                name: "PARTICIPANT_DEMOGRAPHIC",
                schema: "dbo",
                columns: table => new
                {
                    PARTICIPANT_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NHS_NUMBER = table.Column<long>(type: "bigint", nullable: false),
                    SUPERSEDED_BY_NHS_NUMBER = table.Column<long>(type: "bigint", nullable: true),
                    PRIMARY_CARE_PROVIDER = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PRIMARY_CARE_PROVIDER_FROM_DT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CURRENT_POSTING = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CURRENT_POSTING_FROM_DT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NAME_PREFIX = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GIVEN_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OTHER_GIVEN_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FAMILY_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PREVIOUS_FAMILY_NAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DATE_OF_BIRTH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GENDER = table.Column<short>(type: "smallint", nullable: true),
                    ADDRESS_LINE_1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADDRESS_LINE_2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADDRESS_LINE_3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADDRESS_LINE_4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ADDRESS_LINE_5 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    POST_CODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PAF_KEY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    USUAL_ADDRESS_FROM_DT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DATE_OF_DEATH = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DEATH_STATUS = table.Column<short>(type: "smallint", nullable: true),
                    TELEPHONE_NUMBER_HOME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TELEPHONE_NUMBER_HOME_FROM_DT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TELEPHONE_NUMBER_MOB = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TELEPHONE_NUMBER_MOB_FROM_DT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EMAIL_ADDRESS_HOME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EMAIL_ADDRESS_HOME_FROM_DT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PREFERRED_LANGUAGE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    INTERPRETER_REQUIRED = table.Column<short>(type: "smallint", nullable: true),
                    INVALID_FLAG = table.Column<short>(type: "smallint", nullable: true),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPANT_DEMOGRAPHIC", x => x.PARTICIPANT_ID);
                });

            migrationBuilder.CreateTable(
                name: "PARTICIPANT_MANAGEMENT",
                schema: "dbo",
                columns: table => new
                {
                    PARTICIPANT_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SCREENING_ID = table.Column<long>(type: "bigint", nullable: false),
                    NHS_NUMBER = table.Column<long>(type: "bigint", nullable: false),
                    RECORD_TYPE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ELIGIBILITY_FLAG = table.Column<short>(type: "smallint", nullable: false),
                    REASON_FOR_REMOVAL = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    REASON_FOR_REMOVAL_FROM_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BUSINESS_RULE_VERSION = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EXCEPTION_FLAG = table.Column<short>(type: "smallint", nullable: false),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NEXT_TEST_DUE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NEXT_TEST_DUE_DATE_CALC_METHOD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PARTICIPANT_SCREENING_STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCREENING_CEASED_REASON = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IS_HIGHER_RISK = table.Column<short>(type: "smallint", nullable: true),
                    IS_HIGHER_RISK_ACTIVE = table.Column<short>(type: "smallint", nullable: true),
                    HIGHER_RISK_NEXT_TEST_DUE_DATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HIGHER_RISK_REFERRAL_REASON_ID = table.Column<int>(type: "int", nullable: true),
                    DATE_IRRADIATED = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GENE_CODE_ID = table.Column<int>(type: "int", nullable: true),
                    SRC_SYSTEM_PROCESSED_DATETIME = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPANT_MANAGEMENT", x => x.PARTICIPANT_ID);
                });

            migrationBuilder.CreateTable(
                name: "SCREENING_LKP",
                schema: "dbo",
                columns: table => new
                {
                    SCREENING_WORKFLOW_ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SCREENING_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SCREENING_NAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCREENING_TYPE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SCREENING_ACRONYM = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCREENING_LKP", x => x.SCREENING_WORKFLOW_ID);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BS_COHORT_DISTRIBUTION",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BS_SELECT_GP_PRACTICE_LKP",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BS_SELECT_OUTCODE_MAPPING_LKP",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CURRENT_POSTING_LKP",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EXCEPTION_MANAGEMENT",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EXCLUDED_SMU_LKP",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "GENE_CODE_LKP",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "GP_PRACTICES",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "HIGHER_RISK_REFERRAL_REASON_LKP",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "LANGUAGE_CODES",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PARTICIPANT_DEMOGRAPHIC",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PARTICIPANT_MANAGEMENT",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SCREENING_LKP",
                schema: "dbo");
        }
    }
