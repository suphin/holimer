using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addTeklifurunTable4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Toplam",
                table: "Teklif",
                newName: "SiparisToplam");

            migrationBuilder.RenameColumn(
                name: "Kdv",
                table: "Teklif",
                newName: "KdvToplam");

            migrationBuilder.RenameColumn(
                name: "Iskonto",
                table: "Teklif",
                newName: "IskontoToplam");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SiparisToplam",
                table: "Teklif",
                newName: "Toplam");

            migrationBuilder.RenameColumn(
                name: "KdvToplam",
                table: "Teklif",
                newName: "Kdv");

            migrationBuilder.RenameColumn(
                name: "IskontoToplam",
                table: "Teklif",
                newName: "Iskonto");
        }
    }
}
