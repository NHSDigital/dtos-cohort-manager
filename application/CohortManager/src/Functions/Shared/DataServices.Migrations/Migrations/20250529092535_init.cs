using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
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
                    SUPERSEDED_NHS_NUMBER = table.Column<long>(type: "bigint", nullable: true),
                    PRIMARY_CARE_PROVIDER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PRIMARY_CARE_PROVIDER_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    NAME_PREFIX = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    GIVEN_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OTHER_GIVEN_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FAMILY_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PREVIOUS_FAMILY_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DATE_OF_BIRTH = table.Column<DateTime>(type: "datetime", nullable: true),
                    GENDER = table.Column<short>(type: "smallint", nullable: false),
                    ADDRESS_LINE_1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_4 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ADDRESS_LINE_5 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    POST_CODE = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    USUAL_ADDRESS_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    CURRENT_POSTING = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CURRENT_POSTING_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    DATE_OF_DEATH = table.Column<DateTime>(type: "datetime", nullable: true),
                    TELEPHONE_NUMBER_HOME = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    TELEPHONE_NUMBER_HOME_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    TELEPHONE_NUMBER_MOB = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    TELEPHONE_NUMBER_MOB_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    EMAIL_ADDRESS_HOME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EMAIL_ADDRESS_HOME_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    PREFERRED_LANGUAGE = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    INTERPRETER_REQUIRED = table.Column<short>(type: "smallint", nullable: false),
                    REASON_FOR_REMOVAL = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    REASON_FOR_REMOVAL_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    IS_EXTRACTED = table.Column<short>(type: "smallint", nullable: false),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
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
                    AUDIT_CREATED_TIMESTAMP = table.Column<DateTime>(type: "datetime", nullable: false),
                    AUDIT_LAST_MODIFIED_TIMESTAMP = table.Column<DateTime>(type: "datetime", nullable: false),
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
                    AUDIT_CREATED_TIMESTAMP = table.Column<DateTime>(type: "datetime", nullable: false),
                    AUDIT_LAST_MODIFIED_TIMESTAMP = table.Column<DateTime>(type: "datetime", nullable: false),
                    AUDIT_TEXT = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BS_SELECT_OUTCODE_MAPPING_LKP", x => x.OUTCODE);
                });

            migrationBuilder.CreateTable(
                name: "BS_SELECT_REQUEST_AUDIT",
                schema: "dbo",
                columns: table => new
                {
                    REQUEST_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    STATUS_CODE = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CREATED_DATETIME = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BS_SELECT_REQUEST_AUDIT", x => x.REQUEST_ID);
                });

            migrationBuilder.CreateTable(
                name: "BSO_ORGANISATIONS",
                schema: "dbo",
                columns: table => new
                {
                    BSO_ORGANISATION_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BSO_ORGANISATION_CODE = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    BSO_ORGANISATION_NAME = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SAFETY_PERIOD = table.Column<byte>(type: "tinyint", nullable: false),
                    RISP_RECALL_INTERVAL = table.Column<byte>(type: "tinyint", nullable: false),
                    TRANSACTION_ID = table.Column<int>(type: "int", nullable: true),
                    TRANSACTION_APP_DATE_TIME = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TRANSACTION_USER_ORG_ROLE_ID = table.Column<int>(type: "int", nullable: true),
                    TRANSACTION_DB_DATE_TIME = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IGNORE_SELF_REFERRALS = table.Column<bool>(type: "bit", nullable: false),
                    IGNORE_GP_REFERRALS = table.Column<bool>(type: "bit", nullable: false),
                    IGNORE_EARLY_RECALL = table.Column<bool>(type: "bit", nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    LOWER_AGE_RANGE = table.Column<byte>(type: "tinyint", nullable: false),
                    UPPER_AGE_RANGE = table.Column<byte>(type: "tinyint", nullable: false),
                    LINK_CODE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FOA_MAX_OFFSET = table.Column<byte>(type: "tinyint", nullable: false),
                    BSO_RECALL_INTERVAL = table.Column<byte>(type: "tinyint", nullable: false),
                    ADDRESS_LINE_1 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_2 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_3 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_4 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_5 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    POSTCODE = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    TELEPHONE_NUMBER = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    EXTENSION = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    FAX_NUMBER = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    EMAIL_ADDRESS = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OUTGOING_TRANSFER_NUMBER = table.Column<int>(type: "int", nullable: false),
                    INVITE_LIST_SEQUENCE_NUMBER = table.Column<int>(type: "int", nullable: false),
                    FAILSAFE_DATE_OF_MONTH = table.Column<byte>(type: "tinyint", nullable: false),
                    FAILSAFE_MONTHS = table.Column<byte>(type: "tinyint", nullable: false),
                    FAILSAFE_MIN_AGE_YEARS = table.Column<byte>(type: "tinyint", nullable: false),
                    FAILSAFE_MIN_AGE_MONTHS = table.Column<byte>(type: "tinyint", nullable: false),
                    FAILSAFE_MAX_AGE_YEARS = table.Column<byte>(type: "tinyint", nullable: false),
                    FAILSAFE_MAX_AGE_MONTHS = table.Column<byte>(type: "tinyint", nullable: false),
                    FAILSAFE_LAST_RUN = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IS_AGEX = table.Column<bool>(type: "bit", nullable: false),
                    IS_AGEX_ACTIVE = table.Column<bool>(type: "bit", nullable: false),
                    AUTO_BATCH_LAST_RUN = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AUTO_BATCH_MAX_DATE_TIME_PROCESSED = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BSO_REGION_ID = table.Column<int>(type: "int", nullable: true),
                    ADMIN_EMAIL_ADDRESS = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IEP_DETAILS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NOTES = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RLP_DATE_ENABLED = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BSO_ORGANISATIONS", x => x.BSO_ORGANISATION_ID);
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
                    IS_FATAL = table.Column<short>(type: "smallint", nullable: true),
                    SERVICENOW_ID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SERVICENOW_CREATED_DATE = table.Column<DateTime>(type: "datetime", nullable: true)
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
                name: "GENDER_MASTER",
                schema: "dbo",
                columns: table => new
                {
                    GENDER_CD = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    GENDER_DESC = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GENDER_MASTER", x => x.GENDER_CD);
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
                    OPEN_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    CLOSE_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    FAILSAFE_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
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
                name: "NEMS_SUBSCRIPTION",
                schema: "dbo",
                columns: table => new
                {
                    SUBSCRIPTION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NHS_NUMBER = table.Column<long>(type: "bigint", nullable: false),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NEMS_SUBSCRIPTION", x => x.SUBSCRIPTION_ID);
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
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true)
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
                    REASON_FOR_REMOVAL_FROM_DT = table.Column<DateTime>(type: "datetime", nullable: true),
                    BUSINESS_RULE_VERSION = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EXCEPTION_FLAG = table.Column<short>(type: "smallint", nullable: false),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    BLOCKED_FLAG = table.Column<short>(type: "smallint", nullable: false),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    NEXT_TEST_DUE_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    NEXT_TEST_DUE_DATE_CALC_METHOD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PARTICIPANT_SCREENING_STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SCREENING_CEASED_REASON = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IS_HIGHER_RISK = table.Column<short>(type: "smallint", nullable: true),
                    IS_HIGHER_RISK_ACTIVE = table.Column<short>(type: "smallint", nullable: true),
                    HIGHER_RISK_NEXT_TEST_DUE_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    HIGHER_RISK_REFERRAL_REASON_ID = table.Column<int>(type: "int", nullable: true),
                    DATE_IRRADIATED = table.Column<DateTime>(type: "datetime", nullable: true),
                    GENE_CODE_ID = table.Column<int>(type: "int", nullable: true),
                    SRC_SYSTEM_PROCESSED_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_BS_COHORT_DISTRIBUTION_NHSNUMBER",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                column: "NHS_NUMBER");

            migrationBuilder.CreateIndex(
                name: "IX_EXCEPTIONMGMT_NHSNUM_SCREENINGNAME",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT",
                columns: new[] { "NHS_NUMBER", "SCREENING_NAME" });

            migrationBuilder.CreateIndex(
                name: "Index_PARTICIPANT_DEMOGRAPHIC_NhsNumber",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                column: "NHS_NUMBER");

            migrationBuilder.CreateIndex(
                name: "ix_PARTICIPANT_MANAGEMENT_screening_nhs",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                columns: new[] { "NHS_NUMBER", "SCREENING_ID" });
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
                name: "BS_SELECT_REQUEST_AUDIT",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BSO_ORGANISATIONS",
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
                name: "GENDER_MASTER",
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
                name: "NEMS_SUBSCRIPTION",
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
}
