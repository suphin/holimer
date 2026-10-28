using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261023120000_AddProductionReservationUnits")]
public sealed class AddProductionReservationUnits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Do NOT backfill from today's material card: legacy reservations may use a historical lot unit.
        migrationBuilder.AddColumn<int>(name: "UnitId", table: "PrdStockReservation", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "RequirementUnitId", table: "PrdStockReservation", type: "int", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "RequirementUnitFactor", table: "PrdStockReservation", type: "decimal(38,18)", precision: 38, scale: 18, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RequirementUnitFactor", table: "PrdStockReservation");
        migrationBuilder.DropColumn(name: "RequirementUnitId", table: "PrdStockReservation");
        migrationBuilder.DropColumn(name: "UnitId", table: "PrdStockReservation");
    }
}
