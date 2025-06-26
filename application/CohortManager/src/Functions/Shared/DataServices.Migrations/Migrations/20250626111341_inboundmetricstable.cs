using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class inboundmetricstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INBOUND_METRICS",
                schema: "dbo",
                columns: table => new
                {
                    METRIC_AUDIT_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PROCESS_NAME = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RECEIVED_DATETIME = table.Column<DateTime>(type: "datetime", nullable: false),
                    SOURCE = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RECORD_COUNT = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INBOUND_METRICS", x => x.METRIC_AUDIT_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INBOUND_METRICS",
                schema: "dbo");
        }
    }
}
