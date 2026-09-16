using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionCustomerOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrdCustomerOrder",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalOrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PrdCustomerOrder", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrdCustomerOrderLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerOrderId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ProductMaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CancelledQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PrdCustomerOrderLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdCustomerOrderLine_PrdCustomerOrder_CustomerOrderId",
                        column: x => x.CustomerOrderId,
                        principalTable: "PrdCustomerOrder",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdCustomerOrderLine_PrdMaterial_ProductMaterialId",
                        column: x => x.ProductMaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdCustomerOrderLine_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrdProductionPlanOrderAllocation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerOrderLineId = table.Column<int>(type: "int", nullable: false),
                    ProductionPlanId = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PrdProductionPlanOrderAllocation", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlanOrderAllocation_PrdCustomerOrderLine_CustomerOrderLineId",
                        column: x => x.CustomerOrderLineId,
                        principalTable: "PrdCustomerOrderLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrdProductionPlanOrderAllocation_PrdProductionPlan_ProductionPlanId",
                        column: x => x.ProductionPlanId,
                        principalTable: "PrdProductionPlan",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrdCustomerOrder_OrderNumber",
                table: "PrdCustomerOrder",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdCustomerOrder_Status_RequestedDeliveryDate",
                table: "PrdCustomerOrder",
                columns: new[] { "Status", "RequestedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdCustomerOrderLine_CustomerOrderId_Sequence",
                table: "PrdCustomerOrderLine",
                columns: new[] { "CustomerOrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdCustomerOrderLine_ProductMaterialId_RequestedDeliveryDate",
                table: "PrdCustomerOrderLine",
                columns: new[] { "ProductMaterialId", "RequestedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PrdCustomerOrderLine_UnitId",
                table: "PrdCustomerOrderLine",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanOrderAllocation_CustomerOrderLineId_ProductionPlanId",
                table: "PrdProductionPlanOrderAllocation",
                columns: new[] { "CustomerOrderLineId", "ProductionPlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrdProductionPlanOrderAllocation_ProductionPlanId",
                table: "PrdProductionPlanOrderAllocation",
                column: "ProductionPlanId");

            migrationBuilder.Sql(
                """
                DECLARE @CategoryId int;
                SELECT TOP (1) @CategoryId = [ID] FROM [AuthorizationCategory]
                WHERE [Ad] = N'Üretim Siparişleri'
                ORDER BY CASE WHEN [IsDelete] = 1 THEN 1 ELSE 0 END, [ID];

                IF @CategoryId IS NULL
                BEGIN
                    INSERT INTO [AuthorizationCategory] ([Ad], [Aciklama], [IsActive], [IsDelete], [CreateDate])
                    VALUES (N'Üretim Siparişleri', N'Üretim öncesi sipariş giriş ve planlama yetkileri', 1, 0, GETDATE());
                    SET @CategoryId = CAST(SCOPE_IDENTITY() AS int);
                END
                ELSE
                    UPDATE [AuthorizationCategory] SET [IsActive] = 1, [IsDelete] = 0 WHERE [ID] = @CategoryId;

                MERGE [Yetkilendirme] AS target
                USING (VALUES
                    (N'Siparişleri Görüntüleme', N'Üretim sipariş listesi ve detaylarını görüntüleme', N'UretimSiparisGoruntule'),
                    (N'Sipariş Oluşturma', N'Yeni üretim siparişi oluşturma', N'UretimSiparisOlustur'),
                    (N'Sipariş Düzenleme', N'Taslak üretim siparişlerini düzenleme', N'UretimSiparisDuzenle'),
                    (N'Siparişi Planlamaya Gönderme', N'Taslak siparişi planlamaya hazır hale getirme', N'UretimSiparisOnayla'),
                    (N'Sipariş Planlama', N'Sipariş kalemlerini üretim planına aktarma', N'UretimSiparisPlanla'),
                    (N'Sipariş İptali', N'Planlanmamış üretim siparişini iptal etme', N'UretimSiparisIptal')
                ) AS source ([Ad], [Aciklama], [ClaimName])
                ON target.[ClaimType] = N'Authorize' AND target.[ClaimName] = source.[ClaimName]
                WHEN MATCHED THEN UPDATE SET [Ad]=source.[Ad], [Aciklama]=source.[Aciklama], [KategoriID]=@CategoryId,
                    [PolicyName]=source.[ClaimName], [IsActive]=1, [IsDelete]=0, [UpdateDate]=GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT ([Ad], [Aciklama], [KategoriID], [PolicyName], [ClaimType], [ClaimName], [IsActive], [IsDelete], [CreateDate])
                    VALUES (source.[Ad], source.[Aciklama], @CategoryId, source.[ClaimName], N'Authorize', source.[ClaimName], 1, 0, GETDATE());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [Yetkilendirme] WHERE [ClaimType]=N'Authorize' AND [ClaimName] IN
                (N'UretimSiparisGoruntule',N'UretimSiparisOlustur',N'UretimSiparisDuzenle',N'UretimSiparisOnayla',N'UretimSiparisPlanla',N'UretimSiparisIptal');
                """);
            migrationBuilder.DropTable(
                name: "PrdProductionPlanOrderAllocation");

            migrationBuilder.DropTable(
                name: "PrdCustomerOrderLine");

            migrationBuilder.DropTable(
                name: "PrdCustomerOrder");
        }
    }
}
