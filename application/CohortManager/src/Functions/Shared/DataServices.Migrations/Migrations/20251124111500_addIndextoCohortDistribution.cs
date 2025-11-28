using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class addIndextoCohortDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BS_COHORT_DISTRIBUTION_PARTICIPANTID",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                column: "PARTICIPANT_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BS_COHORT_DISTRIBUTION_PARTICIPANTID",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION");
        }
    }
}
