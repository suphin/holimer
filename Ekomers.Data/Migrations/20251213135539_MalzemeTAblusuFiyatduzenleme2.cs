using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class MalzemeTAblusuFiyatduzenleme2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SonMaliyetGuncellemeTarih",
                table: "Malzeme",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MalzemeMaliyetFiyat",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MalzemeID = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Maliyet = table.Column<double>(type: "float", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DovizTur = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateUserID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteUserID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DosyaID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateUserID = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MalzemeMaliyetFiyat", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MalzemeMaliyetFiyat_Malzeme_MalzemeID",
                        column: x => x.MalzemeID,
                        principalTable: "Malzeme",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MalzemeMaliyetFiyat_MalzemeID",
                table: "MalzemeMaliyetFiyat",
                column: "MalzemeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MalzemeMaliyetFiyat");

            migrationBuilder.DropColumn(
                name: "SonMaliyetGuncellemeTarih",
                table: "Malzeme");
        }
    }
}
