using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addTeklifurunTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DovizTur",
                table: "TeklifUrunler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DovizTurAd",
                table: "TeklifUrunler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DovizTur",
                table: "TeklifUrunler");

            migrationBuilder.DropColumn(
                name: "DovizTurAd",
                table: "TeklifUrunler");
        }
    }
}
