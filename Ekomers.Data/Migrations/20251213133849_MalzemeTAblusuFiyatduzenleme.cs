using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class MalzemeTAblusuFiyatduzenleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaliyetSatis",
                table: "Malzeme",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonFiyatGuncellemeTarih",
                table: "Malzeme",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaliyetSatis",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "SonFiyatGuncellemeTarih",
                table: "Malzeme");
        }
    }
}
