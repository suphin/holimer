using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrdProductionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdUnit",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_PrdUnit", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrdWarehouse",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdWarehouse", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrdMaterial",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    LogoCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoActive = table.Column<bool>(type: "bit", nullable: false),
                    LogoLastSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdMaterial", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdMaterial_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdRecipe",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ProductMaterialId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdRecipe", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdRecipe_PrdMaterial_ProductMaterialId",
                        column: x => x.ProductMaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdStockLot",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PrdStockLot", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdStockLot_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockLot_PrdWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdRecipeVersion",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdRecipeVersion", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdRecipeVersion_PrdRecipe_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "PrdRecipe",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdRecipeVersion_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdStockMovement",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    TransferNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdStockMovement", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdStockMovement_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockMovement_PrdStockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockMovement_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockMovement_PrdWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdProductionPlan",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipeVersionId = table.Column<int>(type: "int", nullable: false),
                    ProductMaterialId = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    PlannedProductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PrdProductionPlan", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlan_PrdMaterial_ProductMaterialId",
                        column: x => x.ProductMaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlan_PrdRecipeVersion_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "PrdRecipeVersion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlan_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdRecipeItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeVersionId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    PlannedWasteRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    AlternativeGroupCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_PrdRecipeItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdRecipeItem_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdRecipeItem_PrdRecipeVersion_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "PrdRecipeVersion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdRecipeItem_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdProductionOrder",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductionPlanId = table.Column<int>(type: "int", nullable: false),
                    RecipeVersionId = table.Column<int>(type: "int", nullable: false),
                    ProductMaterialId = table.Column<int>(type: "int", nullable: false),
                    SourceWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductionWarehouseId = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlannedProductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PrdProductionOrder", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdProductionOrder_PrdMaterial_ProductMaterialId",
                        column: x => x.ProductMaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionOrder_PrdProductionPlan_ProductionPlanId",
                        column: x => x.ProductionPlanId,
                        principalTable: "PrdProductionPlan",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionOrder_PrdRecipeVersion_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "PrdRecipeVersion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionOrder_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionOrder_PrdWarehouse_ProductionWarehouseId",
                        column: x => x.ProductionWarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionOrder_PrdWarehouse_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdMaterialRequirement",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    RecipeItemId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    TheoreticalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WasteQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PrdMaterialRequirement", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdMaterialRequirement_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdMaterialRequirement_PrdProductionOrder_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "PrdProductionOrder",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdMaterialRequirement_PrdRecipeItem_RecipeItemId",
                        column: x => x.RecipeItemId,
                        principalTable: "PrdRecipeItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdMaterialRequirement_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdProductionResult",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductMaterialId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StockLotId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PrdProductionResult", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdProductionResult_PrdMaterial_ProductMaterialId",
                        column: x => x.ProductMaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionResult_PrdProductionOrder_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "PrdProductionOrder",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionResult_PrdStockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionResult_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionResult_PrdWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdWarehouseTask",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    SourceWarehouseId = table.Column<int>(type: "int", nullable: false),
                    TargetWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreparedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PrdWarehouseTask", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTask_PrdProductionOrder_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "PrdProductionOrder",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTask_PrdWarehouse_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTask_PrdWarehouse_TargetWarehouseId",
                        column: x => x.TargetWarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdProductionMaterialActual",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    MaterialRequirementId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WasteQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WasteReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PrdProductionMaterialActual", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdProductionMaterialActual_PrdMaterialRequirement_MaterialRequirementId",
                        column: x => x.MaterialRequirementId,
                        principalTable: "PrdMaterialRequirement",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionMaterialActual_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionMaterialActual_PrdProductionOrder_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "PrdProductionOrder",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionMaterialActual_PrdStockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionMaterialActual_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdStockReservation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialRequirementId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UsedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PrdStockReservation", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdStockReservation_PrdMaterialRequirement_MaterialRequirementId",
                        column: x => x.MaterialRequirementId,
                        principalTable: "PrdMaterialRequirement",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockReservation_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockReservation_PrdStockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdStockReservation_PrdWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdWarehouseTaskItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseTaskId = table.Column<int>(type: "int", nullable: false),
                    MaterialRequirementId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PreparedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ShippedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PrdWarehouseTaskItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskItem_PrdMaterialRequirement_MaterialRequirementId",
                        column: x => x.MaterialRequirementId,
                        principalTable: "PrdMaterialRequirement",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskItem_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskItem_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskItem_PrdWarehouseTask_WarehouseTaskId",
                        column: x => x.WarehouseTaskId,
                        principalTable: "PrdWarehouseTask",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdWarehouseTaskLot",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseTaskItemId = table.Column<int>(type: "int", nullable: false),
                    StockReservationId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PrdWarehouseTaskLot", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskLot_PrdStockLot_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "PrdStockLot",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskLot_PrdStockReservation_StockReservationId",
                        column: x => x.StockReservationId,
                        principalTable: "PrdStockReservation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseTaskLot_PrdWarehouseTaskItem_WarehouseTaskItemId",
                        column: x => x.WarehouseTaskItemId,
                        principalTable: "PrdWarehouseTaskItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterial_Code",
                table: "PrdMaterial",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterial_LogoCode",
                table: "PrdMaterial",
                column: "LogoCode");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterial_UnitId",
                table: "PrdMaterial",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialRequirement_MaterialId",
                table: "PrdMaterialRequirement",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialRequirement_ProductionOrderId",
                table: "PrdMaterialRequirement",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialRequirement_RecipeItemId",
                table: "PrdMaterialRequirement",
                column: "RecipeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialRequirement_UnitId",
                table: "PrdMaterialRequirement",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionMaterialActual_MaterialId",
                table: "PrdProductionMaterialActual",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionMaterialActual_MaterialRequirementId",
                table: "PrdProductionMaterialActual",
                column: "MaterialRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionMaterialActual_ProductionOrderId",
                table: "PrdProductionMaterialActual",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionMaterialActual_StockLotId",
                table: "PrdProductionMaterialActual",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionMaterialActual_UnitId",
                table: "PrdProductionMaterialActual",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_OrderNumber",
                table: "PrdProductionOrder",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_ProductionPlanId",
                table: "PrdProductionOrder",
                column: "ProductionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_ProductionWarehouseId",
                table: "PrdProductionOrder",
                column: "ProductionWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_ProductMaterialId",
                table: "PrdProductionOrder",
                column: "ProductMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_RecipeVersionId",
                table: "PrdProductionOrder",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_SourceWarehouseId",
                table: "PrdProductionOrder",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionOrder_UnitId",
                table: "PrdProductionOrder",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlan_PlanNumber",
                table: "PrdProductionPlan",
                column: "PlanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlan_ProductMaterialId",
                table: "PrdProductionPlan",
                column: "ProductMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlan_RecipeVersionId",
                table: "PrdProductionPlan",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlan_UnitId",
                table: "PrdProductionPlan",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionResult_ProductionOrderId",
                table: "PrdProductionResult",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionResult_ProductMaterialId",
                table: "PrdProductionResult",
                column: "ProductMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionResult_StockLotId",
                table: "PrdProductionResult",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionResult_UnitId",
                table: "PrdProductionResult",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionResult_WarehouseId",
                table: "PrdProductionResult",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipe_Code",
                table: "PrdRecipe",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipe_ProductMaterialId",
                table: "PrdRecipe",
                column: "ProductMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeItem_MaterialId",
                table: "PrdRecipeItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeItem_RecipeVersionId_Sequence",
                table: "PrdRecipeItem",
                columns: new[] { "RecipeVersionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeItem_UnitId",
                table: "PrdRecipeItem",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeVersion_RecipeId_VersionNumber",
                table: "PrdRecipeVersion",
                columns: new[] { "RecipeId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeVersion_UnitId",
                table: "PrdRecipeVersion",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockLot_MaterialId_WarehouseId_LotNumber",
                table: "PrdStockLot",
                columns: new[] { "MaterialId", "WarehouseId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockLot_WarehouseId",
                table: "PrdStockLot",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockMovement_MaterialId_WarehouseId_StockLotId_MovementDate",
                table: "PrdStockMovement",
                columns: new[] { "MaterialId", "WarehouseId", "StockLotId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockMovement_StockLotId",
                table: "PrdStockMovement",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockMovement_UnitId",
                table: "PrdStockMovement",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockMovement_WarehouseId",
                table: "PrdStockMovement",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockReservation_MaterialId_WarehouseId_StockLotId_Status",
                table: "PrdStockReservation",
                columns: new[] { "MaterialId", "WarehouseId", "StockLotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockReservation_MaterialRequirementId",
                table: "PrdStockReservation",
                column: "MaterialRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockReservation_StockLotId",
                table: "PrdStockReservation",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdStockReservation_WarehouseId",
                table: "PrdStockReservation",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdUnit_Code",
                table: "PrdUnit",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouse_Code",
                table: "PrdWarehouse",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTask_ProductionOrderId",
                table: "PrdWarehouseTask",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTask_SourceWarehouseId",
                table: "PrdWarehouseTask",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTask_TargetWarehouseId",
                table: "PrdWarehouseTask",
                column: "TargetWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTask_TaskNumber",
                table: "PrdWarehouseTask",
                column: "TaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskItem_MaterialId",
                table: "PrdWarehouseTaskItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskItem_MaterialRequirementId",
                table: "PrdWarehouseTaskItem",
                column: "MaterialRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskItem_UnitId",
                table: "PrdWarehouseTaskItem",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskItem_WarehouseTaskId",
                table: "PrdWarehouseTaskItem",
                column: "WarehouseTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskLot_StockLotId",
                table: "PrdWarehouseTaskLot",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskLot_StockReservationId",
                table: "PrdWarehouseTaskLot",
                column: "StockReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseTaskLot_WarehouseTaskItemId",
                table: "PrdWarehouseTaskLot",
                column: "WarehouseTaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrdProductionMaterialActual");

            migrationBuilder.DropTable(
                name: "PrdProductionResult");

            migrationBuilder.DropTable(
                name: "PrdStockMovement");

            migrationBuilder.DropTable(
                name: "PrdWarehouseTaskLot");

            migrationBuilder.DropTable(
                name: "PrdStockReservation");

            migrationBuilder.DropTable(
                name: "PrdWarehouseTaskItem");

            migrationBuilder.DropTable(
                name: "PrdStockLot");

            migrationBuilder.DropTable(
                name: "PrdMaterialRequirement");

            migrationBuilder.DropTable(
                name: "PrdWarehouseTask");

            migrationBuilder.DropTable(
                name: "PrdRecipeItem");

            migrationBuilder.DropTable(
                name: "PrdProductionOrder");

            migrationBuilder.DropTable(
                name: "PrdProductionPlan");

            migrationBuilder.DropTable(
                name: "PrdWarehouse");

            migrationBuilder.DropTable(
                name: "PrdRecipeVersion");

            migrationBuilder.DropTable(
                name: "PrdRecipe");

            migrationBuilder.DropTable(
                name: "PrdMaterial");

            migrationBuilder.DropTable(
                name: "PrdUnit");
        }
    }
}
