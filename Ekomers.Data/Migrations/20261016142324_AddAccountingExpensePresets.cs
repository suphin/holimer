using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingExpensePresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PresetKey",
                table: "RptAccountingExpenseGroup",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RptAccountingExpenseGroup_PresetKey",
                table: "RptAccountingExpenseGroup",
                column: "PresetKey",
                unique: true,
                filter: "[PresetKey] IS NOT NULL AND [IsDelete] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RptAccountingExpenseGroup_PresetKey",
                table: "RptAccountingExpenseGroup");

            migrationBuilder.DropColumn(
                name: "PresetKey",
                table: "RptAccountingExpenseGroup");
        }
    }
}
