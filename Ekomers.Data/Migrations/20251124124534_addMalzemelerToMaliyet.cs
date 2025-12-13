using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addMalzemelerToMaliyet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Maliyet",
                table: "MalzemeFiyat",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Maliyet",
                table: "Malzeme",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Maliyet",
                table: "MalzemeFiyat");

            migrationBuilder.DropColumn(
                name: "Maliyet",
                table: "Malzeme");
        }
    }
}
