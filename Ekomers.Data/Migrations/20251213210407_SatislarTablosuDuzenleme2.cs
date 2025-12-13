using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class SatislarTablosuDuzenleme2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlatformID",
                table: "Satislar");

            migrationBuilder.AddColumn<string>(
                name: "PersonelID",
                table: "Satislar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonelID",
                table: "Satislar");

            migrationBuilder.AddColumn<int>(
                name: "PlatformID",
                table: "Satislar",
                type: "int",
                nullable: true);
        }
    }
}
