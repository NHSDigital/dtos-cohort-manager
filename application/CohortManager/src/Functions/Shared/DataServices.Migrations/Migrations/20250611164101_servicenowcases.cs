using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class servicenowcases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SERVICENOW_CASES",
                schema: "dbo",
                columns: table => new
                {
                    SERVICENOW_ID = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NHS_NUMBER = table.Column<long>(type: "bigint", nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RECORD_INSERT_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true),
                    RECORD_UPDATE_DATETIME = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SERVICENOW_CASES", x => x.SERVICENOW_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SERVICENOW_CASES",
                schema: "dbo");
        }
    }
}
