using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261002153000_AddLogoConsumptionSlips")]
public sealed class AddLogoConsumptionSlips : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PrdLogoConsumptionSlip",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                LogoSlipNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                LogoAssignedSlipNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                WarehouseNumber = table.Column<int>(type: "int", nullable: false),
                DivisionNumber = table.Column<int>(type: "int", nullable: false),
                DepartmentNumber = table.Column<int>(type: "int", nullable: false),
                FactoryNumber = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LastAttemptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                LogoReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
            constraints: table => table.PrimaryKey("PK_PrdLogoConsumptionSlip", x => x.ID));

        migrationBuilder.CreateTable(
            name: "PrdLogoConsumptionSlipLine",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SlipId = table.Column<int>(type: "int", nullable: false),
                Sequence = table.Column<int>(type: "int", nullable: false),
                MaterialId = table.Column<int>(type: "int", nullable: false),
                UnitId = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                table.PrimaryKey("PK_PrdLogoConsumptionSlipLine", x => x.ID);
                table.ForeignKey("FK_PrdLogoConsumptionSlipLine_PrdLogoConsumptionSlip_SlipId", x => x.SlipId, "PrdLogoConsumptionSlip", "ID", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_PrdLogoConsumptionSlipLine_PrdMaterial_MaterialId", x => x.MaterialId, "PrdMaterial", "ID", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PrdLogoConsumptionSlipLine_PrdUnit_UnitId", x => x.UnitId, "PrdUnit", "ID", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_PrdLogoConsumptionSlip_DocumentNumber", "PrdLogoConsumptionSlip", "DocumentNumber", unique: true);
        migrationBuilder.CreateIndex("IX_PrdLogoConsumptionSlip_Status_DocumentDate", "PrdLogoConsumptionSlip", new[] { "Status", "DocumentDate" });
        migrationBuilder.CreateIndex("IX_PrdLogoConsumptionSlipLine_MaterialId", "PrdLogoConsumptionSlipLine", "MaterialId");
        migrationBuilder.CreateIndex("IX_PrdLogoConsumptionSlipLine_SlipId_Sequence", "PrdLogoConsumptionSlipLine", new[] { "SlipId", "Sequence" }, unique: true);
        migrationBuilder.CreateIndex("IX_PrdLogoConsumptionSlipLine_UnitId", "PrdLogoConsumptionSlipLine", "UnitId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PrdLogoConsumptionSlipLine");
        migrationBuilder.DropTable(name: "PrdLogoConsumptionSlip");
    }
}
