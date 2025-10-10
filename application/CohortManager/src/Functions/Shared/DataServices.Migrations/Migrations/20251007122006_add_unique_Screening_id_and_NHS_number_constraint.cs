using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class add_unique_Screening_id_and_NHS_number_constraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_PARTICIPANT_MANAGEMENT_screening_nhs",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT");

            migrationBuilder.CreateIndex(
                name: "ix_PARTICIPANT_MANAGEMENT_screening_nhs",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                columns: new[] { "NHS_NUMBER", "SCREENING_ID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_PARTICIPANT_MANAGEMENT_screening_nhs",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT");

            migrationBuilder.CreateIndex(
                name: "ix_PARTICIPANT_MANAGEMENT_screening_nhs",
                schema: "dbo",
                table: "PARTICIPANT_MANAGEMENT",
                columns: new[] { "NHS_NUMBER", "SCREENING_ID" });
        }
    }
}
