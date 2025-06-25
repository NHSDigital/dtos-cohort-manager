using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class addnewcolumntoexceptiontable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RECORD_UPDATED_DATE",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT",
                type: "datetime",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RECORD_UPDATED_DATE",
                schema: "dbo",
                table: "EXCEPTION_MANAGEMENT");
        }
    }
}
