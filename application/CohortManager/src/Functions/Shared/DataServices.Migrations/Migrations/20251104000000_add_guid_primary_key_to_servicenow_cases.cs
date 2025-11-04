using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class add_guid_primary_key_to_servicenow_cases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new ID column (GUID) - nullable for now to allow backfilling
            migrationBuilder.AddColumn<Guid>(
                name: "ID",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2: Backfill GUIDs for existing rows
            migrationBuilder.Sql(@"
                UPDATE dbo.SERVICENOW_CASES 
                SET ID = NEWID() 
                WHERE ID IS NULL");

            // Step 3: Make ID column NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "ID",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // Step 4: Drop the old primary key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            // Step 5: Add new primary key on ID column
            migrationBuilder.AddPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "ID");

            // Step 6: Add index on SERVICENOW_ID for query performance
            // Note: Not unique to allow retry scenarios where same case ID may be inserted multiple times
            migrationBuilder.CreateIndex(
                name: "IX_SERVICENOW_CASES_SERVICENOW_ID",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "SERVICENOW_ID");

            // Step 7: Add index on NHS_NUMBER for query performance
            migrationBuilder.CreateIndex(
                name: "IX_SERVICENOW_CASES_NHS_NUMBER",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "NHS_NUMBER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the NHS_NUMBER index
            migrationBuilder.DropIndex(
                name: "IX_SERVICENOW_CASES_NHS_NUMBER",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            // Step 2: Drop the SERVICENOW_ID index
            migrationBuilder.DropIndex(
                name: "IX_SERVICENOW_CASES_SERVICENOW_ID",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            // Step 3: Drop the current primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES");

            // Step 4: Restore old primary key on SERVICENOW_ID
            migrationBuilder.AddPrimaryKey(
                name: "PK_SERVICENOW_CASES",
                schema: "dbo",
                table: "SERVICENOW_CASES",
                column: "SERVICENOW_ID");

            // Step 5: Drop the ID column
            migrationBuilder.DropColumn(
                name: "ID",
                schema: "dbo",
                table: "SERVICENOW_CASES");
        }
    }
}
