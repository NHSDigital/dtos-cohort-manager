using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataServices.Database.Migrations
{
    public partial class add_subscription_source_to_nems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SUBSCRIPTION_SOURCE",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SUBSCRIPTION_SOURCE",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION");
        }
    }
}

