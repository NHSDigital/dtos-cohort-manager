using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class nemssubscriptionstringid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the PK constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_NEMS_SUBSCRIPTION",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION");

            // Alter the column type from Guid to string (nvarchar(450))
            migrationBuilder.AlterColumn<string>(
                name: "SUBSCRIPTION_ID",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // Re-add the PK constraint
            migrationBuilder.AddPrimaryKey(
                name: "PK_NEMS_SUBSCRIPTION",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION",
                column: "SUBSCRIPTION_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new PK constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_NEMS_SUBSCRIPTION",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION");

            // Revert column to Guid (uniqueidentifier)
            migrationBuilder.AlterColumn<Guid>(
                name: "SUBSCRIPTION_ID",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // Re-add original PK
            migrationBuilder.AddPrimaryKey(
                name: "PK_NEMS_SUBSCRIPTION",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION",
                column: "SUBSCRIPTION_ID");
        }
    }
}
