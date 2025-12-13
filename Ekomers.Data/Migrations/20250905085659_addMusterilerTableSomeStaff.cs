using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addMusterilerTableSomeStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CariBakiyeTl",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ParaBirimi",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "Tip",
                table: "Musteriler");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Adres",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Eposta",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ilce",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParaBirimiID",
                table: "Musteriler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostaKod",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipID",
                table: "Musteriler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Ulke",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adres",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "Eposta",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "Ilce",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "ParaBirimiID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "PostaKod",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "TipID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "Ulke",
                table: "Musteriler");

            migrationBuilder.AlterColumn<string>(
                name: "Aciklama",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CariBakiyeTl",
                table: "Musteriler",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParaBirimi",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tip",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
