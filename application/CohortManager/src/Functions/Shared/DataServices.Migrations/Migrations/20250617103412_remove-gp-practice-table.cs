using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataServices.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class removegppracticetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GP_PRACTICES",
                schema: "dbo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GP_PRACTICES",
                schema: "dbo",
                columns: table => new
                {
                    GP_PRACTICE_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ACTIONED = table.Column<bool>(type: "bit", nullable: false),
                    ADDRESS_LINE_1 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_2 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_3 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_4 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    ADDRESS_LINE_5 = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: true),
                    BSO_ORGANISATION_ID = table.Column<int>(type: "int", nullable: false),
                    CLOSE_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    FAILSAFE_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    GP_PRACTICE_CODE = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    GP_PRACTICE_GROUP_ID = table.Column<int>(type: "int", nullable: true),
                    GP_PRACTICE_NAME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LAST_ACTIONED_BY_USER_ORG_ROLE_ID = table.Column<int>(type: "int", nullable: true),
                    LAST_ACTIONED_ON = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LAST_UPDATED_DATE_TIME = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OPEN_DATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    OUTCODE = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    POSTCODE = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    STATUS_CODE = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    TELEPHONE_NUMBER = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    TRANSACTION_APP_DATE_TIME = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TRANSACTION_DB_DATE_TIME = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TRANSACTION_ID = table.Column<int>(type: "int", nullable: false),
                    TRANSACTION_USER_ORG_ROLE_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GP_PRACTICES", x => x.GP_PRACTICE_ID);
                });
        }
    }
}
