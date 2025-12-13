using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addTeklifIskontotable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SiparisToplam",
                table: "Teklif",
                newName: "Toplam");

            migrationBuilder.AddColumn<double>(
                name: "BrutToplam",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SatirIskontoToplam",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TeklifToplam",
                table: "Teklif",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "BrutToplam",
                table: "Siparis",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SatirIskontoToplam",
                table: "Siparis",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Toplam",
                table: "Siparis",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrutToplam",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "SatirIskontoToplam",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "TeklifToplam",
                table: "Teklif");

            migrationBuilder.DropColumn(
                name: "BrutToplam",
                table: "Siparis");

            migrationBuilder.DropColumn(
                name: "SatirIskontoToplam",
                table: "Siparis");

            migrationBuilder.DropColumn(
                name: "Toplam",
                table: "Siparis");

            migrationBuilder.RenameColumn(
                name: "Toplam",
                table: "Teklif",
                newName: "SiparisToplam");
        }
    }
}
