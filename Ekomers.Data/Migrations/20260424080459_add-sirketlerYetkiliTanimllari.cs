using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addsirketlerYetkiliTanimllari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GenelKoordinator",
                table: "Sirketler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenelMudur",
                table: "Sirketler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MuhasebeMuduru",
                table: "Sirketler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SatinalmaMuduru",
                table: "Sirketler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenelKoordinator",
                table: "Sirketler");

            migrationBuilder.DropColumn(
                name: "GenelMudur",
                table: "Sirketler");

            migrationBuilder.DropColumn(
                name: "MuhasebeMuduru",
                table: "Sirketler");

            migrationBuilder.DropColumn(
                name: "SatinalmaMuduru",
                table: "Sirketler");
        }
    }
}
