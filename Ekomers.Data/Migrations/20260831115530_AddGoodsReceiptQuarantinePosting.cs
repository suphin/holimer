using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptQuarantinePosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuarantineInventoryDocumentLineId",
                table: "PurGoodsReceiptLine",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuarantineStockLotId",
                table: "PurGoodsReceiptLine",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuarantineDate",
                table: "PurGoodsReceipt",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuarantineInventoryDocumentId",
                table: "PurGoodsReceipt",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuarantineUserId",
                table: "PurGoodsReceipt",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuarantineWarehouseId",
                table: "PurGoodsReceipt",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurGoodsReceiptLine_QuarantineInventoryDocumentLineId",
                table: "PurGoodsReceiptLine",
                column: "QuarantineInventoryDocumentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurGoodsReceiptLine_QuarantineStockLotId",
                table: "PurGoodsReceiptLine",
                column: "QuarantineStockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PurGoodsReceipt_QuarantineInventoryDocumentId",
                table: "PurGoodsReceipt",
                column: "QuarantineInventoryDocumentId",
                unique: true,
                filter: "[QuarantineInventoryDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurGoodsReceipt_QuarantineWarehouseId",
                table: "PurGoodsReceipt",
                column: "QuarantineWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurGoodsReceipt_PrdInventoryDocument_QuarantineInventoryDocumentId",
                table: "PurGoodsReceipt",
                column: "QuarantineInventoryDocumentId",
                principalTable: "PrdInventoryDocument",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurGoodsReceipt_PrdWarehouse_QuarantineWarehouseId",
                table: "PurGoodsReceipt",
                column: "QuarantineWarehouseId",
                principalTable: "PrdWarehouse",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurGoodsReceiptLine_PrdInventoryDocumentLine_QuarantineInventoryDocumentLineId",
                table: "PurGoodsReceiptLine",
                column: "QuarantineInventoryDocumentLineId",
                principalTable: "PrdInventoryDocumentLine",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurGoodsReceiptLine_PrdStockLot_QuarantineStockLotId",
                table: "PurGoodsReceiptLine",
                column: "QuarantineStockLotId",
                principalTable: "PrdStockLot",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurGoodsReceipt_PrdInventoryDocument_QuarantineInventoryDocumentId",
                table: "PurGoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_PurGoodsReceipt_PrdWarehouse_QuarantineWarehouseId",
                table: "PurGoodsReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_PurGoodsReceiptLine_PrdInventoryDocumentLine_QuarantineInventoryDocumentLineId",
                table: "PurGoodsReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_PurGoodsReceiptLine_PrdStockLot_QuarantineStockLotId",
                table: "PurGoodsReceiptLine");

            migrationBuilder.DropIndex(
                name: "IX_PurGoodsReceiptLine_QuarantineInventoryDocumentLineId",
                table: "PurGoodsReceiptLine");

            migrationBuilder.DropIndex(
                name: "IX_PurGoodsReceiptLine_QuarantineStockLotId",
                table: "PurGoodsReceiptLine");

            migrationBuilder.DropIndex(
                name: "IX_PurGoodsReceipt_QuarantineInventoryDocumentId",
                table: "PurGoodsReceipt");

            migrationBuilder.DropIndex(
                name: "IX_PurGoodsReceipt_QuarantineWarehouseId",
                table: "PurGoodsReceipt");

            migrationBuilder.DropColumn(
                name: "QuarantineInventoryDocumentLineId",
                table: "PurGoodsReceiptLine");

            migrationBuilder.DropColumn(
                name: "QuarantineStockLotId",
                table: "PurGoodsReceiptLine");

            migrationBuilder.DropColumn(
                name: "QuarantineDate",
                table: "PurGoodsReceipt");

            migrationBuilder.DropColumn(
                name: "QuarantineInventoryDocumentId",
                table: "PurGoodsReceipt");

            migrationBuilder.DropColumn(
                name: "QuarantineUserId",
                table: "PurGoodsReceipt");

            migrationBuilder.DropColumn(
                name: "QuarantineWarehouseId",
                table: "PurGoodsReceipt");
        }
    }
}
