using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class datetime_migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SERVICENOW_CREATED_DATE",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_RESOLVED",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SERVICENOW_CREATED_DATE",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DATE_RESOLVED",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);
        }
    }
}
