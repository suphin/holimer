using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrdProductionPlanningStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsConvertedToOrder",
                table: "PrdProductionPlan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductionPlanHeaderId",
                table: "PrdProductionPlan",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrdProductionPlanHeader",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetProductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CalculatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdProductionPlanHeader", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrdProductionPlanRequirement",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionPlanHeaderId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    TheoreticalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PlannedWasteQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalRequiredQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PhysicalStockQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AvailableStockQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CalculationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PrdProductionPlanRequirement", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlanRequirement_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlanRequirement_PrdProductionPlanHeader_ProductionPlanHeaderId",
                        column: x => x.ProductionPlanHeaderId,
                        principalTable: "PrdProductionPlanHeader",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlanRequirement_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlan_ProductionPlanHeaderId",
                table: "PrdProductionPlan",
                column: "ProductionPlanHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanHeader_PlanNumber",
                table: "PrdProductionPlanHeader",
                column: "PlanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanRequirement_MaterialId",
                table: "PrdProductionPlanRequirement",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanRequirement_ProductionPlanHeaderId_MaterialId_UnitId",
                table: "PrdProductionPlanRequirement",
                columns: new[] { "ProductionPlanHeaderId", "MaterialId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanRequirement_UnitId",
                table: "PrdProductionPlanRequirement",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrdProductionPlan_PrdProductionPlanHeader_ProductionPlanHeaderId",
                table: "PrdProductionPlan",
                column: "ProductionPlanHeaderId",
                principalTable: "PrdProductionPlanHeader",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrdProductionPlan_PrdProductionPlanHeader_ProductionPlanHeaderId",
                table: "PrdProductionPlan");

            migrationBuilder.DropTable(
                name: "PrdProductionPlanRequirement");

            migrationBuilder.DropTable(
                name: "PrdProductionPlanHeader");

            migrationBuilder.DropIndex(
                name: "IX_PrdProductionPlan_ProductionPlanHeaderId",
                table: "PrdProductionPlan");

            migrationBuilder.DropColumn(
                name: "IsConvertedToOrder",
                table: "PrdProductionPlan");

            migrationBuilder.DropColumn(
                name: "ProductionPlanHeaderId",
                table: "PrdProductionPlan");
        }
    }
}
