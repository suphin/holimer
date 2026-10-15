using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261005143000_AddScheduledSqlTasks")]
public sealed class AddScheduledSqlTasks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ScheduledSqlTask",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                SqlScript = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ConnectionTarget = table.Column<int>(type: "int", nullable: false),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                FirstRunTime = table.Column<TimeSpan>(type: "time", nullable: false),
                LastRunTime = table.Column<TimeSpan>(type: "time", nullable: false),
                IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                DaysOfWeekMask = table.Column<int>(type: "int", nullable: false),
                RunsPerDay = table.Column<int>(type: "int", nullable: false),
                CommandTimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastSucceeded = table.Column<bool>(type: "bit", nullable: true),
                LastAffectedRows = table.Column<int>(type: "int", nullable: true),
                LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                IsRunning = table.Column<bool>(type: "bit", nullable: false),
                RunningSince = table.Column<DateTime>(type: "datetime2", nullable: true),
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
            constraints: table => table.PrimaryKey("PK_ScheduledSqlTask", x => x.ID));

        migrationBuilder.CreateTable(
            name: "ScheduledSqlTaskRun",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TaskId = table.Column<int>(type: "int", nullable: false),
                ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                AffectedRows = table.Column<int>(type: "int", nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                TriggerType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                TriggeredBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                table.PrimaryKey("PK_ScheduledSqlTaskRun", x => x.ID);
                table.ForeignKey(
                    name: "FK_ScheduledSqlTaskRun_ScheduledSqlTask_TaskId",
                    column: x => x.TaskId,
                    principalTable: "ScheduledSqlTask",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ScheduledSqlTask_IsEnabled_IsDelete_NextRunAt",
            table: "ScheduledSqlTask",
            columns: new[] { "IsEnabled", "IsDelete", "NextRunAt" });
        migrationBuilder.CreateIndex(
            name: "IX_ScheduledSqlTaskRun_StartedAt",
            table: "ScheduledSqlTaskRun",
            column: "StartedAt");
        migrationBuilder.CreateIndex(
            name: "IX_ScheduledSqlTaskRun_TaskId",
            table: "ScheduledSqlTaskRun",
            column: "TaskId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ScheduledSqlTaskRun");
        migrationBuilder.DropTable(name: "ScheduledSqlTask");
    }
}
