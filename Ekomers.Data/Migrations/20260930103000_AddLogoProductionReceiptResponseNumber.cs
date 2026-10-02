using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260930103000_AddLogoProductionReceiptResponseNumber")]
public sealed class AddLogoProductionReceiptResponseNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LogoAssignedSlipNumber",
            table: "PrdLogoProductionReceipt",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LogoAssignedSlipNumber",
            table: "PrdLogoProductionReceipt");
    }
}
