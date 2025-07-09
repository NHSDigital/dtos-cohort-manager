using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class removeceasedflag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CEASED_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "CEASED_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
