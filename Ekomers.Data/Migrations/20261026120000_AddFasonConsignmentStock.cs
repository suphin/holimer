using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFasonConsignmentStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @CategoryId int;
                SELECT TOP(1) @CategoryId=ID FROM AuthorizationCategory WHERE Ad=N'Üretim' ORDER BY CASE WHEN IsDelete=1 THEN 1 ELSE 0 END,ID;
                IF @CategoryId IS NULL
                BEGIN
                    INSERT AuthorizationCategory(Ad,Aciklama,IsActive,IsDelete,CreateDate) VALUES(N'Üretim',N'Üretim modülü işlem yetkileri',1,0,GETDATE());
                    SET @CategoryId=CONVERT(int,SCOPE_IDENTITY());
                END
                ELSE UPDATE AuthorizationCategory SET IsActive=1,IsDelete=0 WHERE ID=@CategoryId;
                MERGE Yetkilendirme AS target USING(VALUES
                    (N'Fason Hammadde Yönetimi',N'Firmaya özel emanet depo açma, hammadde/ambalaj giriş taslağı oluşturma ve firma stoğuna alma',N'FasonHammaddeYonet'),
                    (N'Fason Hammadde Geri Alma',N'Firmaya ait emanet hammadde girişini gerekçeli ters hareketle geri alma',N'FasonHammaddeGeriAl')
                ) AS source(Ad,Aciklama,ClaimName)
                ON target.ClaimType=N'Authorize' AND target.ClaimName=source.ClaimName
                WHEN MATCHED THEN UPDATE SET Ad=source.Ad,Aciklama=source.Aciklama,KategoriID=@CategoryId,PolicyName=source.ClaimName,IsActive=1,IsDelete=0,UpdateDate=GETDATE()
                WHEN NOT MATCHED THEN INSERT(Ad,Aciklama,KategoriID,PolicyName,ClaimType,ClaimName,IsActive,IsDelete,CreateDate)
                    VALUES(source.Ad,source.Aciklama,@CategoryId,source.ClaimName,N'Authorize',source.ClaimName,1,0,GETDATE());
                """);
            migrationBuilder.CreateTable(
                name: "FsnWarehouse",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
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
                    table.PrimaryKey("PK_FsnWarehouse", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "FsnReceipt",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispatchNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedUser = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReversedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedUser = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_FsnReceipt", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FsnReceipt_FsnWarehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "FsnWarehouse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FsnReceiptLine",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalUnitId = table.Column<int>(type: "int", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_FsnReceiptLine", x => x.ID);
                    table.CheckConstraint("CK_FsnReceiptLine_Positive", "[Quantity] > 0 AND [OriginalQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_FsnReceiptLine_FsnReceipt_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "FsnReceipt",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FsnReceiptLine_PrdMaterial_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "PrdMaterial",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FsnReceiptLine_PrdUnit_OriginalUnitId",
                        column: x => x.OriginalUnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FsnReceiptLine_PrdUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PrdUnit",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FsnStockMovement",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptLineId = table.Column<int>(type: "int", nullable: false),
                    IsReversal = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_FsnStockMovement", x => x.ID);
                    table.CheckConstraint("CK_FsnStockMovement_Positive", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_FsnStockMovement_FsnReceiptLine_ReceiptLineId",
                        column: x => x.ReceiptLineId,
                        principalTable: "FsnReceiptLine",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceipt_Number",
                table: "FsnReceipt",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceipt_RequestId",
                table: "FsnReceipt",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceipt_WarehouseId_ReceiptDate",
                table: "FsnReceipt",
                columns: new[] { "WarehouseId", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceiptLine_MaterialId_LotNumber",
                table: "FsnReceiptLine",
                columns: new[] { "MaterialId", "LotNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceiptLine_OriginalUnitId",
                table: "FsnReceiptLine",
                column: "OriginalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceiptLine_ReceiptId_Sequence",
                table: "FsnReceiptLine",
                columns: new[] { "ReceiptId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FsnReceiptLine_UnitId",
                table: "FsnReceiptLine",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_FsnStockMovement_MovementDate",
                table: "FsnStockMovement",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_FsnStockMovement_ReceiptLineId_IsReversal",
                table: "FsnStockMovement",
                columns: new[] { "ReceiptLineId", "IsReversal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FsnWarehouse_Code",
                table: "FsnWarehouse",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FsnWarehouse_CustomerCode",
                table: "FsnWarehouse",
                column: "CustomerCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Yetkilendirme SET IsActive=0,IsDelete=1,UpdateDate=GETDATE() WHERE ClaimType=N'Authorize' AND ClaimName IN(N'FasonHammaddeYonet',N'FasonHammaddeGeriAl');");
            migrationBuilder.DropTable(
                name: "FsnStockMovement");

            migrationBuilder.DropTable(
                name: "FsnReceiptLine");

            migrationBuilder.DropTable(
                name: "FsnReceipt");

            migrationBuilder.DropTable(
                name: "FsnWarehouse");
        }
    }
}
