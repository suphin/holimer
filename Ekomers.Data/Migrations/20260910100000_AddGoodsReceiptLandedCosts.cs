using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260910100000_AddGoodsReceiptLandedCosts")]
    public partial class AddGoodsReceiptLandedCosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurGoodsReceiptLandedCost",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllocationMethod = table.Column<int>(type: "int", nullable: false),
                    FreightCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CustomsCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InsuranceCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    HandlingLaborCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OtherCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InventoryDocumentId = table.Column<int>(type: "int", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_PurGoodsReceiptLandedCost", x => x.ID);
                    table.CheckConstraint("CK_PurGoodsReceiptLandedCost_Amounts", "[FreightCostTry] >= 0 AND [CustomsCostTry] >= 0 AND [InsuranceCostTry] >= 0 AND [HandlingLaborCostTry] >= 0 AND [OtherCostTry] >= 0 AND [TotalCostTry] > 0");
                    table.ForeignKey(
                        name: "FK_PurGoodsReceiptLandedCost_PrdInventoryDocument_InventoryDocumentId",
                        column: x => x.InventoryDocumentId,
                        principalTable: "PrdInventoryDocument",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurGoodsReceiptLandedCost_PurGoodsReceipt_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "PurGoodsReceipt",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurGoodsReceiptLandedCostAllocation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LandedCostId = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    BaseLineValueTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AllocationRate = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false),
                    AllocatedCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    InventoryDocumentLineId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PurGoodsReceiptLandedCostAllocation", x => x.ID);
                    table.CheckConstraint("CK_PurGoodsReceiptLandedCostAllocation_Amounts", "[BaseLineValueTry] >= 0 AND [AllocationRate] >= 0 AND [AllocatedCostTry] >= 0");
                    table.ForeignKey(name: "FK_PurGoodsReceiptLandedCostAllocation_PrdInventoryDocumentLine_InventoryDocumentLineId", column: x => x.InventoryDocumentLineId, principalTable: "PrdInventoryDocumentLine", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_PurGoodsReceiptLandedCostAllocation_PrdMaterial_MaterialId", column: x => x.MaterialId, principalTable: "PrdMaterial", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_PurGoodsReceiptLandedCostAllocation_PrdStockLot_StockLotId", column: x => x.StockLotId, principalTable: "PrdStockLot", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_PurGoodsReceiptLandedCostAllocation_PrdWarehouse_WarehouseId", column: x => x.WarehouseId, principalTable: "PrdWarehouse", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_PurGoodsReceiptLandedCostAllocation_PurGoodsReceiptLine_GoodsReceiptLineId", column: x => x.GoodsReceiptLineId, principalTable: "PurGoodsReceiptLine", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_PurGoodsReceiptLandedCostAllocation_PurGoodsReceiptLandedCost_LandedCostId", column: x => x.LandedCostId, principalTable: "PurGoodsReceiptLandedCost", principalColumn: "ID", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCost_DocumentNumber", table: "PurGoodsReceiptLandedCost", column: "DocumentNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCost_GoodsReceiptId", table: "PurGoodsReceiptLandedCost", column: "GoodsReceiptId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCost_InventoryDocumentId", table: "PurGoodsReceiptLandedCost", column: "InventoryDocumentId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCostAllocation_GoodsReceiptLineId", table: "PurGoodsReceiptLandedCostAllocation", column: "GoodsReceiptLineId");
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCostAllocation_InventoryDocumentLineId", table: "PurGoodsReceiptLandedCostAllocation", column: "InventoryDocumentLineId");
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCostAllocation_MaterialId", table: "PurGoodsReceiptLandedCostAllocation", column: "MaterialId");
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCostAllocation_StockLotId", table: "PurGoodsReceiptLandedCostAllocation", column: "StockLotId");
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCostAllocation_WarehouseId_StockLotId", table: "PurGoodsReceiptLandedCostAllocation", columns: new[] { "WarehouseId", "StockLotId" });
            migrationBuilder.CreateIndex(name: "IX_PurGoodsReceiptLandedCostAllocation_LandedCostId_GoodsReceiptLineId", table: "PurGoodsReceiptLandedCostAllocation", columns: new[] { "LandedCostId", "GoodsReceiptLineId" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PurGoodsReceiptLandedCostAllocation");
            migrationBuilder.DropTable(name: "PurGoodsReceiptLandedCost");
        }
    }
}
