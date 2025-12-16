using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class satislarOranEkle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MusteriOran",
                table: "Satislar",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PersonelOran",
                table: "Satislar",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MusteriOran",
                table: "Satislar");

            migrationBuilder.DropColumn(
                name: "PersonelOran",
                table: "Satislar");
        }
    }
}
