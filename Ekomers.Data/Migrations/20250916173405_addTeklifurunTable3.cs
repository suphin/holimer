using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addTeklifurunTable3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DolarKuru",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EuroKuru",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Iskonto",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Kdv",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Toplam",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DolarKuru",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "EuroKuru",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "Iskonto",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "Kdv",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "Toplam",
                table: "Teklif");
        }
    }
}
