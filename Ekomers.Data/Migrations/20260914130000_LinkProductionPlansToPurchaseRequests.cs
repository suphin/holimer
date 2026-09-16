using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260914130000_LinkProductionPlansToPurchaseRequests")]
    public partial class LinkProductionPlansToPurchaseRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseRequestCreatedDate",
                table: "PrdProductionPlanHeader",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseRequestId",
                table: "PrdProductionPlanHeader",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseRequestNumber",
                table: "PrdProductionPlanHeader",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanHeader_PurchaseRequestId",
                table: "PrdProductionPlanHeader",
                column: "PurchaseRequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrdProductionPlanHeader_PurchaseRequestId",
                table: "PrdProductionPlanHeader");

            migrationBuilder.DropColumn(name: "PurchaseRequestCreatedDate", table: "PrdProductionPlanHeader");
            migrationBuilder.DropColumn(name: "PurchaseRequestId", table: "PrdProductionPlanHeader");
            migrationBuilder.DropColumn(name: "PurchaseRequestNumber", table: "PrdProductionPlanHeader");
        }
    }
}
