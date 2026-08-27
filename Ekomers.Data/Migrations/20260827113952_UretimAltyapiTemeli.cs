using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class UretimAltyapiTemeli : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BaslamaTarihi",
                table: "Uretim",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BitisTarihi",
                table: "Uretim",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Durum",
                table: "Uretim",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KaynakDepoID",
                table: "Uretim",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanlananUretimTarihi",
                table: "Uretim",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceteVersiyonNo",
                table: "Uretim",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "UretimDepoID",
                table: "Uretim",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UretimEmriNo",
                table: "Uretim",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternatifGrupKodu",
                table: "ReceteUrunler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirimID",
                table: "ReceteUrunler",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SiraNo",
                table: "ReceteUrunler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Zorunlu",
                table: "ReceteUrunler",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BazMiktar",
                table: "Recete",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "BirimID",
                table: "Recete",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Durum",
                table: "Recete",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GecerlilikBaslangic",
                table: "Recete",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GecerlilikBitis",
                table: "Recete",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kod",
                table: "Recete",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OncekiVersiyonID",
                table: "Recete",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersiyonNo",
                table: "Recete",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SktTarih",
                table: "MalzemeStok",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "BelgeID",
                table: "MalzemeStok",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BelgeTuru",
                table: "MalzemeStok",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StokRezervasyonID",
                table: "MalzemeStok",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferNo",
                table: "MalzemeStok",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UretimID",
                table: "MalzemeStok",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepoTuru",
                table: "MalzemeDepo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KaynakTuru",
                table: "Malzeme",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LogoAktif",
                table: "Malzeme",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoKod",
                table: "Malzeme",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogoSonSenkronTarihi",
                table: "Malzeme",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DepoHazirlamaEmri",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UretimID = table.Column<int>(type: "int", nullable: false),
                    KaynakDepoID = table.Column<int>(type: "int", nullable: false),
                    HedefDepoID = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    AtananKullaniciID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TalepTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HazirlanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SevkTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeslimTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DepoHazirlamaEmri", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaEmri_MalzemeDepo_HedefDepoID",
                        column: x => x.HedefDepoID,
                        principalTable: "MalzemeDepo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaEmri_MalzemeDepo_KaynakDepoID",
                        column: x => x.KaynakDepoID,
                        principalTable: "MalzemeDepo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaEmri_Uretim_UretimID",
                        column: x => x.UretimID,
                        principalTable: "Uretim",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UretimEmriMalzeme",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UretimID = table.Column<int>(type: "int", nullable: false),
                    MalzemeID = table.Column<int>(type: "int", nullable: false),
                    BirimID = table.Column<int>(type: "int", nullable: true),
                    ReceteKalemID = table.Column<int>(type: "int", nullable: false),
                    ReceteMiktari = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TeorikMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RezerveMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SevkEdilenMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    GercekTuketimMiktari = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IadeMiktari = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FireMiktari = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AciklanamayanFark = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_UretimEmriMalzeme", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UretimEmriMalzeme_MalzemeBirim_BirimID",
                        column: x => x.BirimID,
                        principalTable: "MalzemeBirim",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimEmriMalzeme_Malzeme_MalzemeID",
                        column: x => x.MalzemeID,
                        principalTable: "Malzeme",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimEmriMalzeme_ReceteUrunler_ReceteKalemID",
                        column: x => x.ReceteKalemID,
                        principalTable: "ReceteUrunler",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimEmriMalzeme_Uretim_UretimID",
                        column: x => x.UretimID,
                        principalTable: "Uretim",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepoHazirlamaKalem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepoHazirlamaEmriID = table.Column<int>(type: "int", nullable: false),
                    UretimEmriMalzemeID = table.Column<int>(type: "int", nullable: false),
                    MalzemeID = table.Column<int>(type: "int", nullable: false),
                    IstenenMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HazirlananMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SevkEdilenMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EksikMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DepoHazirlamaKalem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaKalem_DepoHazirlamaEmri_DepoHazirlamaEmriID",
                        column: x => x.DepoHazirlamaEmriID,
                        principalTable: "DepoHazirlamaEmri",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaKalem_Malzeme_MalzemeID",
                        column: x => x.MalzemeID,
                        principalTable: "Malzeme",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaKalem_UretimEmriMalzeme_UretimEmriMalzemeID",
                        column: x => x.UretimEmriMalzemeID,
                        principalTable: "UretimEmriMalzeme",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokRezervasyon",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UretimID = table.Column<int>(type: "int", nullable: false),
                    UretimEmriMalzemeID = table.Column<int>(type: "int", nullable: false),
                    MalzemeID = table.Column<int>(type: "int", nullable: false),
                    DepoID = table.Column<int>(type: "int", nullable: false),
                    LotNumara = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SktTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RezerveMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    KullanilanMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SerbestBirakilanMiktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StokRezervasyon", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StokRezervasyon_MalzemeDepo_DepoID",
                        column: x => x.DepoID,
                        principalTable: "MalzemeDepo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokRezervasyon_Malzeme_MalzemeID",
                        column: x => x.MalzemeID,
                        principalTable: "Malzeme",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokRezervasyon_UretimEmriMalzeme_UretimEmriMalzemeID",
                        column: x => x.UretimEmriMalzemeID,
                        principalTable: "UretimEmriMalzeme",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokRezervasyon_Uretim_UretimID",
                        column: x => x.UretimID,
                        principalTable: "Uretim",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepoHazirlamaKalemLot",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepoHazirlamaKalemID = table.Column<int>(type: "int", nullable: false),
                    StokRezervasyonID = table.Column<int>(type: "int", nullable: true),
                    LotNumara = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SktTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Miktar = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_DepoHazirlamaKalemLot", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaKalemLot_DepoHazirlamaKalem_DepoHazirlamaKalemID",
                        column: x => x.DepoHazirlamaKalemID,
                        principalTable: "DepoHazirlamaKalem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepoHazirlamaKalemLot_StokRezervasyon_StokRezervasyonID",
                        column: x => x.StokRezervasyonID,
                        principalTable: "StokRezervasyon",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_KaynakDepoID",
                table: "Uretim",
                column: "KaynakDepoID");

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_UretimDepoID",
                table: "Uretim",
                column: "UretimDepoID");

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_UretimEmriNo",
                table: "Uretim",
                column: "UretimEmriNo",
                unique: true,
                filter: "[UretimEmriNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReceteUrunler_BirimID",
                table: "ReceteUrunler",
                column: "BirimID");

            migrationBuilder.CreateIndex(
                name: "IX_Recete_BirimID",
                table: "Recete",
                column: "BirimID");

            migrationBuilder.CreateIndex(
                name: "IX_Recete_Kod_VersiyonNo",
                table: "Recete",
                columns: new[] { "Kod", "VersiyonNo" },
                unique: true,
                filter: "[Kod] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Recete_OncekiVersiyonID",
                table: "Recete",
                column: "OncekiVersiyonID");

            migrationBuilder.CreateIndex(
                name: "IX_MalzemeStok_StokRezervasyonID",
                table: "MalzemeStok",
                column: "StokRezervasyonID");

            migrationBuilder.CreateIndex(
                name: "IX_MalzemeStok_UretimID",
                table: "MalzemeStok",
                column: "UretimID");

            migrationBuilder.CreateIndex(
                name: "IX_Malzeme_LogoKod",
                table: "Malzeme",
                column: "LogoKod",
                unique: true,
                filter: "[LogoKod] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaEmri_HedefDepoID",
                table: "DepoHazirlamaEmri",
                column: "HedefDepoID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaEmri_KaynakDepoID",
                table: "DepoHazirlamaEmri",
                column: "KaynakDepoID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaEmri_UretimID",
                table: "DepoHazirlamaEmri",
                column: "UretimID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaKalem_DepoHazirlamaEmriID",
                table: "DepoHazirlamaKalem",
                column: "DepoHazirlamaEmriID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaKalem_MalzemeID",
                table: "DepoHazirlamaKalem",
                column: "MalzemeID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaKalem_UretimEmriMalzemeID",
                table: "DepoHazirlamaKalem",
                column: "UretimEmriMalzemeID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaKalemLot_DepoHazirlamaKalemID",
                table: "DepoHazirlamaKalemLot",
                column: "DepoHazirlamaKalemID");

            migrationBuilder.CreateIndex(
                name: "IX_DepoHazirlamaKalemLot_StokRezervasyonID",
                table: "DepoHazirlamaKalemLot",
                column: "StokRezervasyonID");

            migrationBuilder.CreateIndex(
                name: "IX_StokRezervasyon_DepoID",
                table: "StokRezervasyon",
                column: "DepoID");

            migrationBuilder.CreateIndex(
                name: "IX_StokRezervasyon_MalzemeID_DepoID_LotNumara_SktTarih_Durum",
                table: "StokRezervasyon",
                columns: new[] { "MalzemeID", "DepoID", "LotNumara", "SktTarih", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_StokRezervasyon_UretimEmriMalzemeID",
                table: "StokRezervasyon",
                column: "UretimEmriMalzemeID");

            migrationBuilder.CreateIndex(
                name: "IX_StokRezervasyon_UretimID",
                table: "StokRezervasyon",
                column: "UretimID");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmriMalzeme_BirimID",
                table: "UretimEmriMalzeme",
                column: "BirimID");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmriMalzeme_MalzemeID",
                table: "UretimEmriMalzeme",
                column: "MalzemeID");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmriMalzeme_ReceteKalemID",
                table: "UretimEmriMalzeme",
                column: "ReceteKalemID");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmriMalzeme_UretimID",
                table: "UretimEmriMalzeme",
                column: "UretimID");

            migrationBuilder.AddForeignKey(
                name: "FK_MalzemeStok_StokRezervasyon_StokRezervasyonID",
                table: "MalzemeStok",
                column: "StokRezervasyonID",
                principalTable: "StokRezervasyon",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MalzemeStok_Uretim_UretimID",
                table: "MalzemeStok",
                column: "UretimID",
                principalTable: "Uretim",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recete_MalzemeBirim_BirimID",
                table: "Recete",
                column: "BirimID",
                principalTable: "MalzemeBirim",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recete_Recete_OncekiVersiyonID",
                table: "Recete",
                column: "OncekiVersiyonID",
                principalTable: "Recete",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceteUrunler_MalzemeBirim_BirimID",
                table: "ReceteUrunler",
                column: "BirimID",
                principalTable: "MalzemeBirim",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Uretim_MalzemeDepo_KaynakDepoID",
                table: "Uretim",
                column: "KaynakDepoID",
                principalTable: "MalzemeDepo",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Uretim_MalzemeDepo_UretimDepoID",
                table: "Uretim",
                column: "UretimDepoID",
                principalTable: "MalzemeDepo",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MalzemeStok_StokRezervasyon_StokRezervasyonID",
                table: "MalzemeStok");

            migrationBuilder.DropForeignKey(
                name: "FK_MalzemeStok_Uretim_UretimID",
                table: "MalzemeStok");

            migrationBuilder.DropForeignKey(
                name: "FK_Recete_MalzemeBirim_BirimID",
                table: "Recete");

            migrationBuilder.DropForeignKey(
                name: "FK_Recete_Recete_OncekiVersiyonID",
                table: "Recete");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceteUrunler_MalzemeBirim_BirimID",
                table: "ReceteUrunler");

            migrationBuilder.DropForeignKey(
                name: "FK_Uretim_MalzemeDepo_KaynakDepoID",
                table: "Uretim");

            migrationBuilder.DropForeignKey(
                name: "FK_Uretim_MalzemeDepo_UretimDepoID",
                table: "Uretim");

            migrationBuilder.DropTable(
                name: "DepoHazirlamaKalemLot");

            migrationBuilder.DropTable(
                name: "DepoHazirlamaKalem");

            migrationBuilder.DropTable(
                name: "StokRezervasyon");

            migrationBuilder.DropTable(
                name: "DepoHazirlamaEmri");

            migrationBuilder.DropTable(
                name: "UretimEmriMalzeme");

            migrationBuilder.DropIndex(
                name: "IX_Uretim_KaynakDepoID",
                table: "Uretim");

            migrationBuilder.DropIndex(
                name: "IX_Uretim_UretimDepoID",
                table: "Uretim");

            migrationBuilder.DropIndex(
                name: "IX_Uretim_UretimEmriNo",
                table: "Uretim");

            migrationBuilder.DropIndex(
                name: "IX_ReceteUrunler_BirimID",
                table: "ReceteUrunler");

            migrationBuilder.DropIndex(
                name: "IX_Recete_BirimID",
                table: "Recete");

            migrationBuilder.DropIndex(
                name: "IX_Recete_Kod_VersiyonNo",
                table: "Recete");

            migrationBuilder.DropIndex(
                name: "IX_Recete_OncekiVersiyonID",
                table: "Recete");

            migrationBuilder.DropIndex(
                name: "IX_MalzemeStok_StokRezervasyonID",
                table: "MalzemeStok");

            migrationBuilder.DropIndex(
                name: "IX_MalzemeStok_UretimID",
                table: "MalzemeStok");

            migrationBuilder.DropIndex(
                name: "IX_Malzeme_LogoKod",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "BaslamaTarihi",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "BitisTarihi",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "KaynakDepoID",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "PlanlananUretimTarihi",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "ReceteVersiyonNo",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "UretimDepoID",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "UretimEmriNo",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "AlternatifGrupKodu",
                table: "ReceteUrunler");

            migrationBuilder.DropColumn(
                name: "BirimID",
                table: "ReceteUrunler");

            migrationBuilder.DropColumn(
                name: "SiraNo",
                table: "ReceteUrunler");

            migrationBuilder.DropColumn(
                name: "Zorunlu",
                table: "ReceteUrunler");

            migrationBuilder.DropColumn(
                name: "BazMiktar",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "BirimID",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "GecerlilikBaslangic",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "GecerlilikBitis",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "Kod",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "OncekiVersiyonID",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "VersiyonNo",
                table: "Recete");

            migrationBuilder.DropColumn(
                name: "BelgeID",
                table: "MalzemeStok");

            migrationBuilder.DropColumn(
                name: "BelgeTuru",
                table: "MalzemeStok");

            migrationBuilder.DropColumn(
                name: "StokRezervasyonID",
                table: "MalzemeStok");

            migrationBuilder.DropColumn(
                name: "TransferNo",
                table: "MalzemeStok");

            migrationBuilder.DropColumn(
                name: "UretimID",
                table: "MalzemeStok");

            migrationBuilder.DropColumn(
                name: "DepoTuru",
                table: "MalzemeDepo");

            migrationBuilder.DropColumn(
                name: "KaynakTuru",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "LogoAktif",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "LogoKod",
                table: "Malzeme");

            migrationBuilder.DropColumn(
                name: "LogoSonSenkronTarihi",
                table: "Malzeme");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SktTarih",
                table: "MalzemeStok",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
