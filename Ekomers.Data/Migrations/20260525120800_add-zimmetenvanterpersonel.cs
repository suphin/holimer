using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class addzimmetenvanterpersonel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KullaniciID",
                table: "Zimmet",
                newName: "PersonelID");

            migrationBuilder.AddColumn<bool>(
                name: "Zimmetli",
                table: "Envanter",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Zimmetli",
                table: "Envanter");

            migrationBuilder.RenameColumn(
                name: "PersonelID",
                table: "Zimmet",
                newName: "KullaniciID");
        }
    }
}
