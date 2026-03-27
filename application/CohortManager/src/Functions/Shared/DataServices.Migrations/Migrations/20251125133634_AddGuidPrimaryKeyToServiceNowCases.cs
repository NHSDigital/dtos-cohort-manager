using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGuidPrimaryKeyToServiceNowCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            migrationBuilder.AddColumn<Guid>(
                name: "ID",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICENOW_CASES_SERVICENOW_ID",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "SERVICENOW_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            migrationBuilder.DropIndex(
                name: "IX_SERVICENOW_CASES_SERVICENOW_ID",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            migrationBuilder.DropColumn(
                name: "ID",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "SERVICENOW_ID");
        }
    }
}
