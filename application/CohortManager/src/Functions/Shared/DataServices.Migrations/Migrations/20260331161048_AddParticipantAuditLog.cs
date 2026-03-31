using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PARTICIPANT_AUDIT_LOG",
                schema: "dbo",
                columns: table => new
                {
                    AUDIT_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CORRELATION_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                        .Annotation("SqlServer:DefaultValueSql", "newid()"),
                    BATCH_ID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NHS_NUMBER = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CREATED_DATETIME = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                        .Annotation("SqlServer:DefaultValueSql", "SYSUTCDATETIME()"),
                    RECORD_SOURCE = table.Column<int>(type: "int", nullable: false),
                    RECORD_SOURCE_DESC = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CREATED_BY = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SCREENING_ID = table.Column<int>(type: "int", nullable: true),
                    RAW_DATA_REF = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPANT_AUDIT_LOG", x => x.AUDIT_ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_BATCH",
                schema: "dbo",
                table: "PARTICIPANT_AUDIT_LOG",
                columns: new[] { "BATCH_ID", "CREATED_DATETIME" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_CORRELATION",
                schema: "dbo",
                table: "PARTICIPANT_AUDIT_LOG",
                column: "CORRELATION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_NHS_NUMBER",
                schema: "dbo",
                table: "PARTICIPANT_AUDIT_LOG",
                columns: new[] { "NHS_NUMBER", "CREATED_DATETIME" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_SOURCE",
                schema: "dbo",
                table: "PARTICIPANT_AUDIT_LOG",
                columns: new[] { "RECORD_SOURCE", "CREATED_DATETIME" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PARTICIPANT_AUDIT_LOG",
                schema: "dbo");
        }
    }
}
