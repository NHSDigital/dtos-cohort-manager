using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class add_subscription_source_to_nems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SUBSCRIPTION_SOURCE",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SUBSCRIPTION_SOURCE",
                schema: "dbo",
                table: "NEMS_SUBSCRIPTION");
        }
    }
}
