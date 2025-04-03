using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Database.Migrations
{
    /// <inheritdoc />
    public partial class nullableSuperceededByNHSNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "SUPERSEDED_NHS_NUMBER",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "PRIMARY_CARE_PROVIDER",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "SUPERSEDED_NHS_NUMBER",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PRIMARY_CARE_PROVIDER",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
