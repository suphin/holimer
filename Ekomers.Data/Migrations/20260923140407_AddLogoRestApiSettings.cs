using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoRestApiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogoRestApiSetting",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    RestUserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RestPasswordProtected = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FirmNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientSecretProtected = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LastTestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    LastTestMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogoRestApiSetting", x => x.ID);
                    table.CheckConstraint("CK_LogoRestApiSetting_Port", "[Port] BETWEEN 1 AND 65535");
                    table.CheckConstraint("CK_LogoRestApiSetting_Protocol", "[Protocol] IN ('HTTP', 'HTTPS')");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogoRestApiSetting");
        }
    }
}
