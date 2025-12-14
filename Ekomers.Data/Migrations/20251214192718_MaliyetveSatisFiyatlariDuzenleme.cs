using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class MaliyetveSatisFiyatlariDuzenleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Fiyat",
                table: "MalzemeMaliyetFiyat",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FiyatSatis",
                table: "Malzeme",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fiyat",
                table: "MalzemeMaliyetFiyat");

            migrationBuilder.DropColumn(
                name: "FiyatSatis",
                table: "Malzeme");
        }
    }
}
