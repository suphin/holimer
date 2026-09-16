using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260908110000_AddSystemErrorLogs")]
    public partial class AddSystemErrorLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ControllerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RemoteIpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExceptionDetails = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrorLogs_OccurredAt",
                table: "SystemErrorLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrorLogs_TrackingNumber",
                table: "SystemErrorLogs",
                column: "TrackingNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SystemErrorLogs");
        }
    }
}
