#nullable disable

namespace DataServices.Migrations.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddGuidPrimaryKeyToServiceNowCases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the existing primary key constraint
        migrationBuilder.DropPrimaryKey(
            name: "PK_SERVICENOW_CASES",
            schema: "dbo",
            table: "SERVICENOW_CASES");

        // Add the new ID column with GUID type
        migrationBuilder.AddColumn<Guid>(
            name: "ID",
            schema: "dbo",
            table: "SERVICENOW_CASES",
            type: "uniqueidentifier",
            nullable: false,
            defaultValueSql: "NEWID()");

        // Backfill existing rows with GUID values (already done by defaultValueSql)
        // This ensures all existing rows get a unique GUID value

        // Add the new primary key on the ID column
        migrationBuilder.AddPrimaryKey(
            name: "PK_SERVICENOW_CASES",
            schema: "dbo",
            table: "SERVICENOW_CASES",
            column: "ID");

        // Add an index on SERVICENOW_ID to maintain query performance
        migrationBuilder.CreateIndex(
            name: "IX_SERVICENOW_CASES_SERVICENOW_ID",
            schema: "dbo",
            table: "SERVICENOW_CASES",
            column: "SERVICENOW_ID");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop the index on SERVICENOW_ID
        migrationBuilder.DropIndex(
            name: "IX_SERVICENOW_CASES_SERVICENOW_ID",
            schema: "dbo",
            table: "SERVICENOW_CASES");

        // Drop the primary key constraint on ID
        migrationBuilder.DropPrimaryKey(
            name: "PK_SERVICENOW_CASES",
            schema: "dbo",
            table: "SERVICENOW_CASES");

        // Drop the ID column
        migrationBuilder.DropColumn(
            name: "ID",
            schema: "dbo",
            table: "SERVICENOW_CASES");

        // Restore the original primary key on SERVICENOW_ID
        migrationBuilder.AddPrimaryKey(
            name: "PK_SERVICENOW_CASES",
            schema: "dbo",
            table: "SERVICENOW_CASES",
            column: "SERVICENOW_ID");
    }
}
