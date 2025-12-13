using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addMusterilerTableSomeStaff3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IlceID",
                table: "Musteriler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mahalle",
                table: "Musteriler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MahalleID",
                table: "Musteriler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SehirID",
                table: "Musteriler",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IlceID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "Mahalle",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "MahalleID",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "SehirID",
                table: "Musteriler");
        }
    }
}
