using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoProductionEntryReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdLogoProductionReceipt",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogoSlipNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarehouseNumber = table.Column<int>(type: "int", nullable: false),
                    DivisionNumber = table.Column<int>(type: "int", nullable: false),
                    DepartmentNumber = table.Column<int>(type: "int", nullable: false),
                    FactoryNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LogoReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PrdLogoProductionReceipt", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrdLogoProductionReceiptLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_PrdLogoProductionReceiptLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdLogoProductionReceiptLine_PrdLogoProductionReceipt_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "PrdLogoProductionReceipt",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrdLogoProductionReceiptLine_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdLogoProductionReceiptLine_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdLogoProductionReceipt_DocumentNumber",
                table: "PrdLogoProductionReceipt",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdLogoProductionReceipt_Status_DocumentDate",
                table: "PrdLogoProductionReceipt",
                columns: new[] { "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdLogoProductionReceiptLine_MaterialId",
                table: "PrdLogoProductionReceiptLine",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdLogoProductionReceiptLine_ReceiptId_Sequence",
                table: "PrdLogoProductionReceiptLine",
                columns: new[] { "ReceiptId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdLogoProductionReceiptLine_UnitId",
                table: "PrdLogoProductionReceiptLine",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrdLogoProductionReceiptLine");

            migrationBuilder.DropTable(
                name: "PrdLogoProductionReceipt");
        }
    }
}
