using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseQualityInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurQualityInspection",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SampleNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SampleDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SampledUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LaboratoryName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResultSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SpecificationNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_PurQualityInspection", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurQualityInspection_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQualityInspection_PrdStockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQualityInspection_PrdWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQualityInspection_PurGoodsReceiptLine_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "PurGoodsReceiptLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQualityInspection_PurGoodsReceipt_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "PurGoodsReceipt",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_GoodsReceiptId",
                table: "PurQualityInspection",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_GoodsReceiptLineId",
                table: "PurQualityInspection",
                column: "GoodsReceiptLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_InspectionNumber",
                table: "PurQualityInspection",
                column: "InspectionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_MaterialId_StockLotId",
                table: "PurQualityInspection",
                columns: new[] { "MaterialId", "StockLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_Status_SampleDate",
                table: "PurQualityInspection",
                columns: new[] { "Status", "SampleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_StockLotId",
                table: "PurQualityInspection",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_WarehouseId",
                table: "PurQualityInspection",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurQualityInspection");
        }
    }
}
