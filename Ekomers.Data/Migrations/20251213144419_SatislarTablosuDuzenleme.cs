using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class SatislarTablosuDuzenleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeklifID",
                table: "SatislarUrunler");

            migrationBuilder.DropColumn(
                name: "FirsatID",
                table: "Satislar");

            migrationBuilder.DropColumn(
                name: "IadeSonuc",
                table: "Satislar");

            migrationBuilder.DropColumn(
                name: "SebepID",
                table: "Satislar");

            migrationBuilder.DropColumn(
                name: "TeklifID",
                table: "Satislar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeklifID",
                table: "SatislarUrunler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FirsatID",
                table: "Satislar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IadeSonuc",
                table: "Satislar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SebepID",
                table: "Satislar",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeklifID",
                table: "Satislar",
                type: "int",
                nullable: true);
        }
    }
}
