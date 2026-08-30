using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewPurchasingOffersAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurSupplier",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxOffice = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PurSupplier", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PurSupplierQuotation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseRequestId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    SupplierQuotationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuotationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentTerms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryTerms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NetTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_PurSupplierQuotation", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurSupplierQuotation_PurPurchaseRequest_PurchaseRequestId",
                        column: x => x.PurchaseRequestId,
                        principalTable: "PurPurchaseRequest",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurSupplierQuotation_PurSupplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "PurSupplier",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurPurchaseOrder",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    SourceQuotationId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PaymentTerms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryTerms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PurPurchaseOrder", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrder_PurSupplierQuotation_SourceQuotationId",
                        column: x => x.SourceQuotationId,
                        principalTable: "PurSupplierQuotation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrder_PurSupplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "PurSupplier",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurSupplierQuotationLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierQuotationId = table.Column<int>(type: "int", nullable: false),
                    PurchaseRequestLineId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    OfferedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovalNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_PurSupplierQuotationLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurSupplierQuotationLine_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurSupplierQuotationLine_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurSupplierQuotationLine_PurPurchaseRequestLine_PurchaseRequestLineId",
                        column: x => x.PurchaseRequestLineId,
                        principalTable: "PurPurchaseRequestLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurSupplierQuotationLine_PurSupplierQuotation_SupplierQuotationId",
                        column: x => x.SupplierQuotationId,
                        principalTable: "PurSupplierQuotation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurPurchaseOrderLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    SupplierQuotationLineId = table.Column<int>(type: "int", nullable: false),
                    PurchaseRequestLineId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TaxTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PurPurchaseOrderLine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrderLine_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrderLine_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrderLine_PurPurchaseOrder_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurPurchaseOrder",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrderLine_PurPurchaseRequestLine_PurchaseRequestLineId",
                        column: x => x.PurchaseRequestLineId,
                        principalTable: "PurPurchaseRequestLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurPurchaseOrderLine_PurSupplierQuotationLine_SupplierQuotationLineId",
                        column: x => x.SupplierQuotationLineId,
                        principalTable: "PurSupplierQuotationLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurQuotationApprovalHistory",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierQuotationId = table.Column<int>(type: "int", nullable: false),
                    SupplierQuotationLineId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    OfferedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PreviousApprovedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_PurQuotationApprovalHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PurQuotationApprovalHistory_PurSupplierQuotationLine_SupplierQuotationLineId",
                        column: x => x.SupplierQuotationLineId,
                        principalTable: "PurSupplierQuotationLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurQuotationApprovalHistory_PurSupplierQuotation_SupplierQuotationId",
                        column: x => x.SupplierQuotationId,
                        principalTable: "PurSupplierQuotation",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrder_OrderNumber",
                table: "PurPurchaseOrder",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrder_SourceQuotationId",
                table: "PurPurchaseOrder",
                column: "SourceQuotationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrder_SupplierId",
                table: "PurPurchaseOrder",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrderLine_MaterialId",
                table: "PurPurchaseOrderLine",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrderLine_PurchaseOrderId_Sequence",
                table: "PurPurchaseOrderLine",
                columns: new[] { "PurchaseOrderId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrderLine_PurchaseRequestLineId_Status",
                table: "PurPurchaseOrderLine",
                columns: new[] { "PurchaseRequestLineId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrderLine_SupplierQuotationLineId",
                table: "PurPurchaseOrderLine",
                column: "SupplierQuotationLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurPurchaseOrderLine_UnitId",
                table: "PurPurchaseOrderLine",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurQuotationApprovalHistory_SupplierQuotationId_SupplierQuotationLineId_ActionDate",
                table: "PurQuotationApprovalHistory",
                columns: new[] { "SupplierQuotationId", "SupplierQuotationLineId", "ActionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurQuotationApprovalHistory_SupplierQuotationLineId",
                table: "PurQuotationApprovalHistory",
                column: "SupplierQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplier_Code",
                table: "PurSupplier",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplier_TaxNumber",
                table: "PurSupplier",
                column: "TaxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotation_PurchaseRequestId_SupplierId_Status",
                table: "PurSupplierQuotation",
                columns: new[] { "PurchaseRequestId", "SupplierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotation_QuotationNumber",
                table: "PurSupplierQuotation",
                column: "QuotationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotation_SupplierId",
                table: "PurSupplierQuotation",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotationLine_MaterialId",
                table: "PurSupplierQuotationLine",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotationLine_PurchaseRequestLineId_Status",
                table: "PurSupplierQuotationLine",
                columns: new[] { "PurchaseRequestLineId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotationLine_SupplierQuotationId_Sequence",
                table: "PurSupplierQuotationLine",
                columns: new[] { "SupplierQuotationId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_PurSupplierQuotationLine_UnitId",
                table: "PurSupplierQuotationLine",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurPurchaseOrderLine");

            migrationBuilder.DropTable(
                name: "PurQuotationApprovalHistory");

            migrationBuilder.DropTable(
                name: "PurPurchaseOrder");

            migrationBuilder.DropTable(
                name: "PurSupplierQuotationLine");

            migrationBuilder.DropTable(
                name: "PurSupplierQuotation");

            migrationBuilder.DropTable(
                name: "PurSupplier");
        }
    }
}
