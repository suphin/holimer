using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionResultCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LaborCost",
                table: "PrdProductionResult",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialCost",
                table: "PrdProductionResult",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCost",
                table: "PrdProductionResult",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherCostDescription",
                table: "PrdProductionResult",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProductionCost",
                table: "PrdProductionResult",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportationCost",
                table: "PrdProductionResult",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitProductionCost",
                table: "PrdProductionResult",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaborCost",
                table: "PrdProductionResult");

            migrationBuilder.DropColumn(
                name: "MaterialCost",
                table: "PrdProductionResult");

            migrationBuilder.DropColumn(
                name: "OtherCost",
                table: "PrdProductionResult");

            migrationBuilder.DropColumn(
                name: "OtherCostDescription",
                table: "PrdProductionResult");

            migrationBuilder.DropColumn(
                name: "TotalProductionCost",
                table: "PrdProductionResult");

            migrationBuilder.DropColumn(
                name: "TransportationCost",
                table: "PrdProductionResult");

            migrationBuilder.DropColumn(
                name: "UnitProductionCost",
                table: "PrdProductionResult");
        }
    }
}
