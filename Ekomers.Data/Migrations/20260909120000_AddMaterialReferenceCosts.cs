using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260909120000_AddMaterialReferenceCosts")]
    public partial class AddMaterialReferenceCosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdMaterialReferenceCost",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitCostTry = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "date", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PrdMaterialReferenceCost", x => x.ID);
                    table.CheckConstraint("CK_PrdMaterialReferenceCost_Amounts", "[UnitCost] > 0 AND [ExchangeRate] > 0 AND [UnitCostTry] > 0");
                    table.CheckConstraint("CK_PrdMaterialReferenceCost_DateRange", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.CheckConstraint("CK_PrdMaterialReferenceCost_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_PrdMaterialReferenceCost_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdMaterialReferenceCost_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialReferenceCost_MaterialId_ValidFrom_ValidTo",
                table: "PrdMaterialReferenceCost",
                columns: new[] { "MaterialId", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialReferenceCost_MaterialId_VersionNumber",
                table: "PrdMaterialReferenceCost",
                columns: new[] { "MaterialId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialReferenceCost_UnitId",
                table: "PrdMaterialReferenceCost",
                column: "UnitId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PrdMaterialReferenceCost");
        }
    }
}
