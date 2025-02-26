using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Database.Migrations
{
    /// <inheritdoc />
    public partial class addAddressLine2ToDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ADDRESS_LINE_2",
                schema: "dbo",
                table: "BSO_ORGANISATIONS",
                type: "nvarchar(35)",
                maxLength: 35,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ADDRESS_LINE_2",
                schema: "dbo",
                table: "BSO_ORGANISATIONS");
        }
    }
}
