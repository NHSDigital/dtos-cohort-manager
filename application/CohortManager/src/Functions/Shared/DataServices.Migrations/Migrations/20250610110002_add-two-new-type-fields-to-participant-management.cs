using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class addtwonewtypefieldstoparticipantmanagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "CEASED_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "REFERRAL_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CEASED_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT");

            migrationBuilder.DropColumn(
                name: "REFERRAL_FLAG",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT");
        }
    }
}
