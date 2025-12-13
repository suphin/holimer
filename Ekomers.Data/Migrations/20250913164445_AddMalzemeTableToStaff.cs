using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMalzemeTableToStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Tarih",
                table: "MalzemeFiyat",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DovizTur",
                table: "Malzeme",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Indirim",
                table: "Malzeme",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Kdv",
                table: "Malzeme",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tarih",
                table: "MalzemeFiyat");

            migrationBuilder.DropColumn(
                name: "DovizTur",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "Indirim",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "Kdv",
                table: "Malzeme");
        }
    }
}
