using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class missingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BSCOHORT_IS_EXTACTED_REQUESTID",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION",
                columns: new[] { "IS_EXTRACTED", "REQUEST_ID" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BSCOHORT_IS_EXTACTED_REQUESTID",
                schema: "dbo",
                table: "BS_COHORT_DISTRIBUTION");
        }
    }
}
