using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeCostApiTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiAccessToken",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokenPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UseCount = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ApiAccessToken", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiAccessToken_IsActive_ExpiresAt",
                table: "ApiAccessToken",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiAccessToken_TokenHash",
                table: "ApiAccessToken",
                column: "TokenHash",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiAccessToken");

        }
    }
}
