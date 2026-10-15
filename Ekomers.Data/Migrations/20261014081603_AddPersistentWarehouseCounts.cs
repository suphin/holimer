using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentWarehouseCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdWarehouseCountDraft",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    InventoryDocumentId = table.Column<int>(type: "int", nullable: true),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PrdWarehouseCountDraft", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseCountDraft_PrdInventoryDocument_InventoryDocumentId",
                        column: x => x.InventoryDocumentId,
                        principalTable: "PrdInventoryDocument",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseCountDraft_PrdInventoryDocument_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "PrdInventoryDocument",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseCountDraft_PrdWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "PrdWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdWarehouseCountEntry",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DraftId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    MaterialCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SnapshotQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_PrdWarehouseCountEntry", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseCountEntry_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseCountEntry_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdWarehouseCountEntry_PrdWarehouseCountDraft_DraftId",
                        column: x => x.DraftId,
                        principalTable: "PrdWarehouseCountDraft",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountDraft_InventoryDocumentId",
                table: "PrdWarehouseCountDraft",
                column: "InventoryDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountDraft_OwnerUserId_WarehouseId_SourceDocumentId",
                table: "PrdWarehouseCountDraft",
                columns: new[] { "OwnerUserId", "WarehouseId", "SourceDocumentId" },
                unique: true,
                filter: "[Status] = 0 AND [IsDelete] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountDraft_SourceDocumentId",
                table: "PrdWarehouseCountDraft",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountDraft_WarehouseId",
                table: "PrdWarehouseCountDraft",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountEntry_DraftId_MaterialId",
                table: "PrdWarehouseCountEntry",
                columns: new[] { "DraftId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountEntry_DraftId_RequestId",
                table: "PrdWarehouseCountEntry",
                columns: new[] { "DraftId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountEntry_MaterialId",
                table: "PrdWarehouseCountEntry",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdWarehouseCountEntry_UnitId",
                table: "PrdWarehouseCountEntry",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrdWarehouseCountEntry");

            migrationBuilder.DropTable(
                name: "PrdWarehouseCountDraft");
        }
    }
}
