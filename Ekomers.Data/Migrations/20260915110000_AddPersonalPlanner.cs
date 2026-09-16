using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915110000_AddPersonalPlanner")]
    public partial class AddPersonalPlanner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalWorkItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                constraints: table => table.PrimaryKey("PK_PersonalWorkItem", x => x.ID));

            migrationBuilder.CreateIndex(
                name: "IX_PersonalWorkItem_OwnerUserId_ItemType_IsDelete",
                table: "PersonalWorkItem",
                columns: new[] { "OwnerUserId", "ItemType", "IsDelete" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalWorkItem_OwnerUserId_Status_StartAt",
                table: "PersonalWorkItem",
                columns: new[] { "OwnerUserId", "Status", "StartAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "PersonalWorkItem");
    }
}
