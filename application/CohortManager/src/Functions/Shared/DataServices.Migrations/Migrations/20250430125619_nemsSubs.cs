using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Database.Migrations
{
    /// <inheritdoc />
    public partial class nemsSubs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SRC_SYSTEM_PROCESSED_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_UPDATE_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_INSERT_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "REASON_FOR_REMOVAL_FROM_DT",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NEXT_TEST_DUE_DATE",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "HIGHER_RISK_NEXT_TEST_DUE_DATE",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_IRRADIATED",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<short>(
                name: "BLOCKED_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_UPDATE_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_INSERT_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OPEN_DATE",
                schema: "dbo",
                table: "GP_PRACTICES",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FAILSAFE_DATE",
                schema: "dbo",
                table: "GP_PRACTICES",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CLOSE_DATE",
                schema: "dbo",
                table: "GP_PRACTICES",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_DATETIME",
                schema: "dbo",
                table: "BS_SELECT_REQUEST_AUDIT",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_LAST_MODIFIED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_OUTCODE_MAPPING_LKP",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_CREATED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_OUTCODE_MAPPING_LKP",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_LAST_MODIFIED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_GP_PRACTICE_LKP",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_CREATED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_GP_PRACTICE_LKP",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "USUAL_ADDRESS_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TELEPHONE_NUMBER_MOB_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TELEPHONE_NUMBER_HOME_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_UPDATE_DATETIME",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_INSERT_DATETIME",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "REASON_FOR_REMOVAL_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PRIMARY_CARE_PROVIDER_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EMAIL_ADDRESS_HOME_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_OF_DEATH",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_OF_BIRTH",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CURRENT_POSTING_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "nemsSubscriptions",
                columns: table => new
                {
                    SUBSCRIPTION_ID = table.Column<long>(type: "bigint", nullable: false),
                    NHS_NUMBER = table.Column<long>(type: "bigint", nullable: false),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nemsSubscriptions", x => x.SUBSCRIPTION_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nemsSubscriptions");

            migrationBuilder.DropColumn(
                name: "BLOCKED_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SRC_SYSTEM_PROCESSED_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_UPDATE_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_INSERT_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "REASON_FOR_REMOVAL_FROM_DT",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NEXT_TEST_DUE_DATE",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "HIGHER_RISK_NEXT_TEST_DUE_DATE",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_IRRADIATED",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_UPDATE_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_INSERT_DATETIME",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OPEN_DATE",
                schema: "dbo",
                table: "GP_PRACTICES",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FAILSAFE_DATE",
                schema: "dbo",
                table: "GP_PRACTICES",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CLOSE_DATE",
                schema: "dbo",
                table: "GP_PRACTICES",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CREATED_DATETIME",
                schema: "dbo",
                table: "BS_SELECT_REQUEST_AUDIT",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_LAST_MODIFIED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_OUTCODE_MAPPING_LKP",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_CREATED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_OUTCODE_MAPPING_LKP",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_LAST_MODIFIED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_GP_PRACTICE_LKP",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AUDIT_CREATED_TIMESTAMP",
                schema: "dbo",
                table: "BS_SELECT_GP_PRACTICE_LKP",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "USUAL_ADDRESS_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TELEPHONE_NUMBER_MOB_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TELEPHONE_NUMBER_HOME_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_UPDATE_DATETIME",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RECORD_INSERT_DATETIME",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "REASON_FOR_REMOVAL_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PRIMARY_CARE_PROVIDER_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EMAIL_ADDRESS_HOME_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_OF_DEATH",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_OF_BIRTH",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CURRENT_POSTING_FROM_DT",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);
        }
    }
}
