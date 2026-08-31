using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionRecipeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdRecipeHistory",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeId = table.Column<int>(type: "int", nullable: false),
                    RecipeVersionId = table.Column<int>(type: "int", nullable: true),
                    RecipeItemId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_PrdRecipeHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdRecipeHistory_PrdRecipeItem_RecipeItemId",
                        column: x => x.RecipeItemId,
                        principalTable: "PrdRecipeItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdRecipeHistory_PrdRecipeVersion_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "PrdRecipeVersion",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdRecipeHistory_PrdRecipe_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "PrdRecipe",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeHistory_RecipeId_RecipeVersionId_ActionDate",
                table: "PrdRecipeHistory",
                columns: new[] { "RecipeId", "RecipeVersionId", "ActionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeHistory_RecipeItemId",
                table: "PrdRecipeHistory",
                column: "RecipeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdRecipeHistory_RecipeVersionId",
                table: "PrdRecipeHistory",
                column: "RecipeVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrdRecipeHistory");
        }
    }
}
