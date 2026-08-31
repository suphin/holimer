using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseTaskLotActualQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PreparedQuantity",
                table: "PrdWarehouseTaskLot",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippedQuantity",
                table: "PrdWarehouseTaskLot",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreparedQuantity",
                table: "PrdWarehouseTaskLot");

            migrationBuilder.DropColumn(
                name: "ShippedQuantity",
                table: "PrdWarehouseTaskLot");
        }
    }
}
