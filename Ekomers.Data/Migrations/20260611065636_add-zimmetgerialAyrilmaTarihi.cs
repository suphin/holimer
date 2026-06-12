using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addzimmetgerialAyrilmaTarihi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AyrilisTarihi",
                table: "Zimmet",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_EnvanterBolum_EnvanterDepartmanID",
                table: "EnvanterBolum",
                column: "EnvanterDepartmanID");

            migrationBuilder.AddForeignKey(
                name: "FK_EnvanterBolum_EnvanterDepartman_EnvanterDepartmanID",
                table: "EnvanterBolum",
                column: "EnvanterDepartmanID",
                principalTable: "EnvanterDepartman",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnvanterBolum_EnvanterDepartman_EnvanterDepartmanID",
                table: "EnvanterBolum");

            migrationBuilder.DropIndex(
                name: "IX_EnvanterBolum_EnvanterDepartmanID",
                table: "EnvanterBolum");

            migrationBuilder.DropColumn(
                name: "AyrilisTarihi",
                table: "Zimmet");
        }
    }
}
