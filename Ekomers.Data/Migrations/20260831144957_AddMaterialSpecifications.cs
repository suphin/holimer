using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SpecificationSetId",
                table: "PurQualityInspection",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrdMaterialSpecificationSet",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    SpecificationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
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
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrdMaterialSpecificationSet", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdMaterialSpecificationSet_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdMaterialSpecificationItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpecificationSetId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MinimumValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MaximumValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ExpectedText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpectedBoolean = table.Column<bool>(type: "bit", nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TestMethod = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Criticality = table.Column<int>(type: "int", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
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
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrdMaterialSpecificationItem", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdMaterialSpecificationItem_PrdMaterialSpecificationSet_SpecificationSetId",
                        column: x => x.SpecificationSetId,
                        principalTable: "PrdMaterialSpecificationSet",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdMaterialSpecificationHistory",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpecificationSetId = table.Column<int>(type: "int", nullable: false),
                    SpecificationItemId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
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
                    table.PrimaryKey("PK_PrdMaterialSpecificationHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdMaterialSpecificationHistory_PrdMaterialSpecificationItem_SpecificationItemId",
                        column: x => x.SpecificationItemId,
                        principalTable: "PrdMaterialSpecificationItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdMaterialSpecificationHistory_PrdMaterialSpecificationSet_SpecificationSetId",
                        column: x => x.SpecificationSetId,
                        principalTable: "PrdMaterialSpecificationSet",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurQualityInspectionSpecificationResult",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QualityInspectionId = table.Column<int>(type: "int", nullable: false),
                    SpecificationSetId = table.Column<int>(type: "int", nullable: false),
                    SpecificationItemId = table.Column<int>(type: "int", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EvaluationNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnalyzedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
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
                    table.PrimaryKey("PK_PurQualityInspectionSpecificationResult", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurQualityInspectionSpecificationResult_PrdMaterialSpecificationItem_SpecificationItemId",
                        column: x => x.SpecificationItemId,
                        principalTable: "PrdMaterialSpecificationItem",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQualityInspectionSpecificationResult_PrdMaterialSpecificationSet_SpecificationSetId",
                        column: x => x.SpecificationSetId,
                        principalTable: "PrdMaterialSpecificationSet",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQualityInspectionSpecificationResult_PurQualityInspection_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalTable: "PurQualityInspection",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspection_SpecificationSetId",
                table: "PurQualityInspection",
                column: "SpecificationSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialSpecificationHistory_SpecificationItemId",
                table: "PrdMaterialSpecificationHistory",
                column: "SpecificationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialSpecificationHistory_SpecificationSetId_ActionDate",
                table: "PrdMaterialSpecificationHistory",
                columns: new[] { "SpecificationSetId", "ActionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialSpecificationItem_SpecificationSetId_Code",
                table: "PrdMaterialSpecificationItem",
                columns: new[] { "SpecificationSetId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialSpecificationItem_SpecificationSetId_Sequence",
                table: "PrdMaterialSpecificationItem",
                columns: new[] { "SpecificationSetId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialSpecificationSet_MaterialId_Status_ValidFrom_ValidTo",
                table: "PrdMaterialSpecificationSet",
                columns: new[] { "MaterialId", "Status", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdMaterialSpecificationSet_MaterialId_VersionNumber",
                table: "PrdMaterialSpecificationSet",
                columns: new[] { "MaterialId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspectionSpecificationResult_QualityInspectionId_SpecificationItemId",
                table: "PurQualityInspectionSpecificationResult",
                columns: new[] { "QualityInspectionId", "SpecificationItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspectionSpecificationResult_QualityInspectionId_Status",
                table: "PurQualityInspectionSpecificationResult",
                columns: new[] { "QualityInspectionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspectionSpecificationResult_SpecificationItemId",
                table: "PurQualityInspectionSpecificationResult",
                column: "SpecificationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurQualityInspectionSpecificationResult_SpecificationSetId",
                table: "PurQualityInspectionSpecificationResult",
                column: "SpecificationSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurQualityInspection_PrdMaterialSpecificationSet_SpecificationSetId",
                table: "PurQualityInspection",
                column: "SpecificationSetId",
                principalTable: "PrdMaterialSpecificationSet",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurQualityInspection_PrdMaterialSpecificationSet_SpecificationSetId",
                table: "PurQualityInspection");

            migrationBuilder.DropTable(
                name: "PrdMaterialSpecificationHistory");

            migrationBuilder.DropTable(
                name: "PurQualityInspectionSpecificationResult");

            migrationBuilder.DropTable(
                name: "PrdMaterialSpecificationItem");

            migrationBuilder.DropTable(
                name: "PrdMaterialSpecificationSet");

            migrationBuilder.DropIndex(
                name: "IX_PurQualityInspection_SpecificationSetId",
                table: "PurQualityInspection");

            migrationBuilder.DropColumn(
                name: "SpecificationSetId",
                table: "PurQualityInspection");
        }
    }
}
