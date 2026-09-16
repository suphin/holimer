using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260914120000_AddUserPageShortcuts")]
    public partial class AddUserPageShortcuts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserID",
                table: "UserShortCut",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageTitle",
                table: "UserShortCut",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageUrl",
                table: "UserShortCut",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "UserShortCut",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserShortCut_UserID_PageUrl",
                table: "UserShortCut",
                columns: new[] { "UserID", "PageUrl" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserShortCut_UserID_PageUrl",
                table: "UserShortCut");

            migrationBuilder.DropColumn(name: "PageTitle", table: "UserShortCut");
            migrationBuilder.DropColumn(name: "PageUrl", table: "UserShortCut");
            migrationBuilder.DropColumn(name: "SortOrder", table: "UserShortCut");

            migrationBuilder.AlterColumn<string>(
                name: "UserID",
                table: "UserShortCut",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
