using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrdInventoryCostFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CostSource",
                table: "PrdStockMovement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "PrdStockMovement",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PrdStockMovement",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InventoryDocumentId",
                table: "PrdStockMovement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InventoryDocumentLineId",
                table: "PrdStockMovement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitCost",
                table: "PrdStockMovement",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "PrdStockMovement",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "PrdStockMovement",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QualityControlRequirement",
                table: "PrdMaterial",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresExpirationDate",
                table: "PrdMaterial",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresLotTracking",
                table: "PrdMaterial",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PrdInventoryDocument",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SourceWarehouseId = table.Column<int>(type: "int", nullable: true),
                    TargetWarehouseId = table.Column<int>(type: "int", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SourceDocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    ReversalDocumentId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdInventoryDocument", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocument_PrdInventoryDocument_ReversalDocumentId",
                        column: x => x.ReversalDocumentId,
                        principalTable: "PrdInventoryDocument",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocument_PrdWarehouse_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocument_PrdWarehouse_TargetWarehouseId",
                        column: x => x.TargetWarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdInventoryDocumentLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryDocumentId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    SourceStockLotId = table.Column<int>(type: "int", nullable: true),
                    TargetStockLotId = table.Column<int>(type: "int", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CostSource = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdInventoryDocumentLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocumentLine_PrdInventoryDocument_InventoryDocumentId",
                        column: x => x.InventoryDocumentId,
                        principalTable: "PrdInventoryDocument",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocumentLine_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocumentLine_PrdStockLot_SourceStockLotId",
                        column: x => x.SourceStockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocumentLine_PrdStockLot_TargetStockLotId",
                        column: x => x.TargetStockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdInventoryDocumentLine_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockMovement_InventoryDocumentId",
                table: "PrdStockMovement",
                column: "InventoryDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockMovement_InventoryDocumentLineId",
                table: "PrdStockMovement",
                column: "InventoryDocumentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocument_DocumentNumber",
                table: "PrdInventoryDocument",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocument_ReversalDocumentId",
                table: "PrdInventoryDocument",
                column: "ReversalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocument_SourceWarehouseId",
                table: "PrdInventoryDocument",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocument_TargetWarehouseId",
                table: "PrdInventoryDocument",
                column: "TargetWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocumentLine_InventoryDocumentId_Sequence",
                table: "PrdInventoryDocumentLine",
                columns: new[] { "InventoryDocumentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocumentLine_MaterialId_SourceStockLotId",
                table: "PrdInventoryDocumentLine",
                columns: new[] { "MaterialId", "SourceStockLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocumentLine_SourceStockLotId",
                table: "PrdInventoryDocumentLine",
                column: "SourceStockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocumentLine_TargetStockLotId",
                table: "PrdInventoryDocumentLine",
                column: "TargetStockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdInventoryDocumentLine_UnitId",
                table: "PrdInventoryDocumentLine",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrdStockMovement_PrdInventoryDocumentLine_InventoryDocumentLineId",
                table: "PrdStockMovement",
                column: "InventoryDocumentLineId",
                principalTable: "PrdInventoryDocumentLine",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PrdStockMovement_PrdInventoryDocument_InventoryDocumentId",
                table: "PrdStockMovement",
                column: "InventoryDocumentId",
                principalTable: "PrdInventoryDocument",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrdStockMovement_PrdInventoryDocumentLine_InventoryDocumentLineId",
                table: "PrdStockMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_PrdStockMovement_PrdInventoryDocument_InventoryDocumentId",
                table: "PrdStockMovement");

            migrationBuilder.DropTable(
                name: "PrdInventoryDocumentLine");

            migrationBuilder.DropTable(
                name: "PrdInventoryDocument");

            migrationBuilder.DropIndex(
                name: "IX_PrdStockMovement_InventoryDocumentId",
                table: "PrdStockMovement");

            migrationBuilder.DropIndex(
                name: "IX_PrdStockMovement_InventoryDocumentLineId",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "CostSource",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "InventoryDocumentId",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "InventoryDocumentLineId",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "OriginalUnitCost",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "PrdStockMovement");

            migrationBuilder.DropColumn(
                name: "QualityControlRequirement",
                table: "PrdMaterial");

            migrationBuilder.DropColumn(
                name: "RequiresExpirationDate",
                table: "PrdMaterial");

            migrationBuilder.DropColumn(
                name: "RequiresLotTracking",
                table: "PrdMaterial");
        }
    }
}
