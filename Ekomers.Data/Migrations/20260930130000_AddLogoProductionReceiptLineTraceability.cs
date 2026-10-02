using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260930130000_AddLogoProductionReceiptLineTraceability")]
public sealed class AddLogoProductionReceiptLineTraceability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LotNumber",
            table: "PrdLogoProductionReceiptLine",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ProductionDate",
            table: "PrdLogoProductionReceiptLine",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpirationDate",
            table: "PrdLogoProductionReceiptLine",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LotNumber",
            table: "PrdLogoProductionReceiptLine");

        migrationBuilder.DropColumn(
            name: "ProductionDate",
            table: "PrdLogoProductionReceiptLine");

        migrationBuilder.DropColumn(
            name: "ExpirationDate",
            table: "PrdLogoProductionReceiptLine");
    }
}
