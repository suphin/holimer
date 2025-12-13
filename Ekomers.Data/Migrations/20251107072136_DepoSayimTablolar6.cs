using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class DepoSayimTablolar6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WarehouseInventories_ProductId",
                table: "WarehouseInventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseInventories_WarehouseId",
                table: "WarehouseInventories",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseInventories_Products_ProductId",
                table: "WarehouseInventories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseInventories_Warehouses_WarehouseId",
                table: "WarehouseInventories",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseInventories_Products_ProductId",
                table: "WarehouseInventories");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseInventories_Warehouses_WarehouseId",
                table: "WarehouseInventories");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseInventories_ProductId",
                table: "WarehouseInventories");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseInventories_WarehouseId",
                table: "WarehouseInventories");
        }
    }
}
