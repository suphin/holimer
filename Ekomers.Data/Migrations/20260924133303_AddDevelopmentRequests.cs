using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDevelopmentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevelopmentRequest",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateUserID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteUserID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DosyaID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateUserID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentRequest", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentRequest_OwnerUserId_Status_IsDelete",
                table: "DevelopmentRequest",
                columns: new[] { "OwnerUserId", "Status", "IsDelete" });

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentRequest_RequestNumber",
                table: "DevelopmentRequest",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentRequest_Status_CreateDate",
                table: "DevelopmentRequest",
                columns: new[] { "Status", "CreateDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevelopmentRequest");
        }
    }
}
