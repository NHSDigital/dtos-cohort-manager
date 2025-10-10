using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InboundMetricReceivedDateTimeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_INBOUND_METRICS_RECEIVEDDATETIME",
                schema: "dbo",
                table: "INBOUND_METRICS",
                column: "RECEIVED_DATETIME");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_INBOUND_METRICS_RECEIVEDDATETIME",
                schema: "dbo",
                table: "INBOUND_METRICS");
        }
    }
}
