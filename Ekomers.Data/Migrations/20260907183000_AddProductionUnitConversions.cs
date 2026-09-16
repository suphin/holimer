using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260907183000_AddProductionUnitConversions")]
    public partial class AddProductionUnitConversions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdUnitConversion",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    FromUnitId = table.Column<int>(type: "int", nullable: false),
                    ToUnitId = table.Column<int>(type: "int", nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PrdUnitConversion", x => x.ID);
                    table.ForeignKey("FK_PrdUnitConversion_PrdMaterial_MaterialId", x => x.MaterialId, "PrdMaterial", "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PrdUnitConversion_PrdUnit_FromUnitId", x => x.FromUnitId, "PrdUnit", "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PrdUnitConversion_PrdUnit_ToUnitId", x => x.ToUnitId, "PrdUnit", "ID", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_PrdUnitConversion_FromUnitId", "PrdUnitConversion", "FromUnitId");
            migrationBuilder.CreateIndex("IX_PrdUnitConversion_ToUnitId", "PrdUnitConversion", "ToUnitId");
            migrationBuilder.CreateIndex(
                "IX_PrdUnitConversion_MaterialId_FromUnitId_ToUnitId",
                "PrdUnitConversion",
                new[] { "MaterialId", "FromUnitId", "ToUnitId" });

            migrationBuilder.CreateTable(
                name: "PrdRecipeCostScenario",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CalculationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DefaultProductionQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FixedCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VariableCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalProductionQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalRecipeCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                constraints: table => table.PrimaryKey("PK_PrdRecipeCostScenario", x => x.ID));
            migrationBuilder.CreateIndex("IX_PrdRecipeCostScenario_ScenarioNumber", "PrdRecipeCostScenario", "ScenarioNumber", unique: true);

            migrationBuilder.CreateTable(
                name: "PrdRecipeCostScenarioLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ScenarioId = table.Column<int>(type: "int", nullable: false),
                    RecipeVersionId = table.Column<int>(type: "int", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ProductionUnit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductionQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RawMaterialUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PackagingUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RecipeMaterialCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VariableCostShare = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FixedCostShare = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PrdRecipeCostScenarioLine", x => x.ID);
                    table.ForeignKey("FK_PrdRecipeCostScenarioLine_PrdRecipeCostScenario_ScenarioId", x => x.ScenarioId, "PrdRecipeCostScenario", "ID", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PrdRecipeCostScenarioLine_PrdRecipeVersion_RecipeVersionId", x => x.RecipeVersionId, "PrdRecipeVersion", "ID", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex("IX_PrdRecipeCostScenarioLine_RecipeVersionId", "PrdRecipeCostScenarioLine", "RecipeVersionId");
            migrationBuilder.CreateIndex("IX_PrdRecipeCostScenarioLine_ScenarioId_RecipeVersionId", "PrdRecipeCostScenarioLine", new[] { "ScenarioId", "RecipeVersionId" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PrdRecipeCostScenarioLine");
            migrationBuilder.DropTable(name: "PrdRecipeCostScenario");
            migrationBuilder.DropTable(name: "PrdUnitConversion");
        }
    }
}
