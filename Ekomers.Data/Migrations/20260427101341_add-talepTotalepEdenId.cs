using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addtalepTotalepEdenId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KabulEdenID",
                table: "RequestUrunler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KabulEdenTarihSaat",
                table: "RequestUrunler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TalepEdenID",
                table: "RequestUrunler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TalepEdenTarihSaat",
                table: "RequestUrunler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KabulEdenID",
                table: "RequestUrunler");

            migrationBuilder.DropColumn(
                name: "KabulEdenTarihSaat",
                table: "RequestUrunler");

            migrationBuilder.DropColumn(
                name: "TalepEdenID",
                table: "RequestUrunler");

            migrationBuilder.DropColumn(
                name: "TalepEdenTarihSaat",
                table: "RequestUrunler");
        }
    }
}
