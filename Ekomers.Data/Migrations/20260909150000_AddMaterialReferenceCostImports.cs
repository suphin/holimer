using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260909150000_AddMaterialReferenceCostImports")]
    public partial class AddMaterialReferenceCostImports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdMaterialReferenceCostImportBatch",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "date", nullable: false),
                    ImportedCount = table.Column<int>(type: "int", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
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
                constraints: table => table.PrimaryKey("PK_PrdMaterialReferenceCostImportBatch", x => x.ID));

            migrationBuilder.AddColumn<int>(name: "ImportBatchId", table: "PrdMaterialReferenceCost", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SourceSheet", table: "PrdMaterialReferenceCost", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<int>(name: "SourceRow", table: "PrdMaterialReferenceCost", type: "int", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialReferenceCostImportBatch_BatchNumber",
                table: "PrdMaterialReferenceCostImportBatch",
                column: "BatchNumber",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialReferenceCost_ImportBatchId",
                table: "PrdMaterialReferenceCost",
                column: "ImportBatchId");
            migrationBuilder.AddForeignKey(
                name: "FK_PrdMaterialReferenceCost_PrdMaterialReferenceCostImportBatch_ImportBatchId",
                table: "PrdMaterialReferenceCost",
                column: "ImportBatchId",
                principalTable: "PrdMaterialReferenceCostImportBatch",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrdMaterialReferenceCost_PrdMaterialReferenceCostImportBatch_ImportBatchId",
                table: "PrdMaterialReferenceCost");
            migrationBuilder.DropIndex(name: "IX_PrdMaterialReferenceCost_ImportBatchId", table: "PrdMaterialReferenceCost");
            migrationBuilder.DropColumn(name: "ImportBatchId", table: "PrdMaterialReferenceCost");
            migrationBuilder.DropColumn(name: "SourceSheet", table: "PrdMaterialReferenceCost");
            migrationBuilder.DropColumn(name: "SourceRow", table: "PrdMaterialReferenceCost");
            migrationBuilder.DropTable(name: "PrdMaterialReferenceCostImportBatch");
        }
    }
}
