using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoBankMovementReportScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogoBankMovementReportSchedule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RecipientUserIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SendTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    LookbackDays = table.Column<int>(type: "int", nullable: false),
                    IncludeToday = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRecordCount = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_LogoBankMovementReportSchedule", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LogoBankMovementReportDelivery",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RangeStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RangeEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanySummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    IncomingTotalTry = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutgoingTotalTry = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogoBankMovementReportDelivery", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogoBankMovementReportDelivery_LogoBankMovementReportSchedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "LogoBankMovementReportSchedule",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogoBankMovementReportDelivery_ScheduleId",
                table: "LogoBankMovementReportDelivery",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_LogoBankMovementReportDelivery_StartedAt",
                table: "LogoBankMovementReportDelivery",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LogoBankMovementReportSchedule_IsEnabled_IsDelete_SendTime",
                table: "LogoBankMovementReportSchedule",
                columns: new[] { "IsEnabled", "IsDelete", "SendTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogoBankMovementReportDelivery");

            migrationBuilder.DropTable(
                name: "LogoBankMovementReportSchedule");
        }
    }
}
