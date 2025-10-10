using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class add_unique_nhs_number_demograpghic_table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Index_PARTICIPANT_DEMOGRAPHIC_NhsNumber",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC");

            migrationBuilder.CreateIndex(
                name: "Index_PARTICIPANT_DEMOGRAPHIC_NhsNumber",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                column: "NHS_NUMBER",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Index_PARTICIPANT_DEMOGRAPHIC_NhsNumber",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC");

            migrationBuilder.CreateIndex(
                name: "Index_PARTICIPANT_DEMOGRAPHIC_NhsNumber",
                schema: "dbo",
                table: "PARTICIPANT_DEMOGRAPHIC",
                column: "NHS_NUMBER");
        }
    }
}
