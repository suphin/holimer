using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915205631_AddSupplierPriceRequestWorkflow")]
    public partial class AddSupplierPriceRequestWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurEmailTemplate",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                constraints: table => table.PrimaryKey("PK_PurEmailTemplate", x => x.ID));

            migrationBuilder.CreateTable(
                name: "PurPriceRequestEmail",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "int", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SenderAccount = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_PurPriceRequestEmail", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurPriceRequestEmail_PurEmailTemplate_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "PurEmailTemplate",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurPriceRequestEmail_PurSupplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "PurSupplier",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurPriceRequestEmailLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceRequestEmailId = table.Column<int>(type: "int", nullable: false),
                    PurchaseRequestLineId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PurPriceRequestEmailLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurPriceRequestEmailLine_PurPriceRequestEmail_PriceRequestEmailId",
                        column: x => x.PriceRequestEmailId,
                        principalTable: "PurPriceRequestEmail",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurPriceRequestEmailLine_PurPurchaseRequestLine_PurchaseRequestLineId",
                        column: x => x.PurchaseRequestLineId,
                        principalTable: "PurPurchaseRequestLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PurEmailTemplate",
                columns: new[] { "ID", "BodyTemplate", "CreateDate", "CreateUserID", "IsActive", "IsDefault", "IsDelete", "Name", "SubjectTemplate" },
                columnTypes: new[] { "int", "nvarchar(max)", "datetime2", "nvarchar(max)", "bit", "bit", "bit", "nvarchar(150)", "nvarchar(500)" },
                values: new object[]
                {
                    1,
                    "<p>Sayın {TEDARIKCI_ADI},</p><p>Aşağıdaki ürünler için fiyat, teslim süresi ve ödeme koşullarınızı içeren teklifinizi rica ederiz.</p>{TALEP_TABLOSU}<p><strong>Teklif son tarihi:</strong> {SON_TARIH}</p><p>{EK_NOT}</p><p>İyi çalışmalar dileriz.</p><p><strong>{GONDEREN_AD_SOYAD}</strong><br />E-posta: {GONDEREN_EPOSTA}<br />Kullanıcı hesabı: {KULLANICI_HESABI}</p>",
                    new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Unspecified),
                    "system",
                    true,
                    true,
                    false,
                    "Standart Fiyat Teklifi İsteği",
                    "Fiyat teklif talebi - {TALEP_NUMARALARI}"
                });

            migrationBuilder.CreateIndex(name: "IX_PurEmailTemplate_Name", table: "PurEmailTemplate", column: "Name", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PurPriceRequestEmail_EmailTemplateId", table: "PurPriceRequestEmail", column: "EmailTemplateId");
            migrationBuilder.CreateIndex(name: "IX_PurPriceRequestEmail_ReferenceNumber", table: "PurPriceRequestEmail", column: "ReferenceNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PurPriceRequestEmail_SupplierId_SentDate", table: "PurPriceRequestEmail", columns: new[] { "SupplierId", "SentDate" });
            migrationBuilder.CreateIndex(name: "IX_PurPriceRequestEmailLine_PriceRequestEmailId_PurchaseRequestLineId", table: "PurPriceRequestEmailLine", columns: new[] { "PriceRequestEmailId", "PurchaseRequestLineId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_PurPriceRequestEmailLine_PurchaseRequestLineId", table: "PurPriceRequestEmailLine", column: "PurchaseRequestLineId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PurPriceRequestEmailLine");
            migrationBuilder.DropTable(name: "PurPriceRequestEmail");
            migrationBuilder.DropTable(name: "PurEmailTemplate");
        }
    }
}
