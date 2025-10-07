#nullable disable

namespace DataServices.Migrations.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class add_unique_Screening_id_and_NHS_number_constraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "screening_id_nhs_number_unique_constraint",
            table: "PARTICIPANT_MANAGEMENT",
            columns: ["NHS_NUMBER", "SCREENING_ID"],
            schema: "dbo"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropUniqueConstraint(
            name: "screening_id_nhs_number_unique_constraint",
            table: "PARTICIPANT_MANAGEMENT",
            schema: "dbo"
        );
    }
}
