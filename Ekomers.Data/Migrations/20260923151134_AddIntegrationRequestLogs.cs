using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationRequestLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationRequestLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConnectionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    RequestContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestHeaders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResponseHeaders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRequestLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestLog_Category_ConnectionName_RequestedAt",
                table: "IntegrationRequestLog",
                columns: new[] { "Category", "ConnectionName", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestLog_IsSuccess_RequestedAt",
                table: "IntegrationRequestLog",
                columns: new[] { "IsSuccess", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestLog_RequestedAt",
                table: "IntegrationRequestLog",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRequestLog_TrackingId",
                table: "IntegrationRequestLog",
                column: "TrackingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationRequestLog");
        }
    }
}
