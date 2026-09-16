using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260910140000_AddInventoryDocumentDeletePermission")]
    public partial class AddInventoryDocumentDeletePermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @ProductionCategoryId int;

                SELECT TOP (1) @ProductionCategoryId = [ID]
                FROM [AuthorizationCategory]
                WHERE [Ad] = N'Üretim'
                ORDER BY CASE WHEN [IsDelete] = 1 THEN 1 ELSE 0 END, [ID];

                IF @ProductionCategoryId IS NULL
                BEGIN
                    INSERT INTO [AuthorizationCategory] ([Ad], [Aciklama], [IsActive], [IsDelete], [CreateDate])
                    VALUES (N'Üretim', N'Üretim modülü işlem yetkileri', 1, 0, GETDATE());
                    SET @ProductionCategoryId = CAST(SCOPE_IDENTITY() AS int);
                END
                ELSE
                    UPDATE [AuthorizationCategory]
                    SET [IsActive] = 1, [IsDelete] = 0
                    WHERE [ID] = @ProductionCategoryId;

                MERGE [Yetkilendirme] AS target
                USING (VALUES
                    (N'Stok Belgesi Silme',
                     N'ProductionInventory ekranındaki bağımsız devir ve transfer belgelerini bağlı stok hareketleriyle kontrollü silme yetkisi',
                     @ProductionCategoryId,
                     N'StokBelgesiSil')
                ) AS source ([Ad], [Aciklama], [KategoriID], [ClaimName])
                ON target.[ClaimType] = N'Authorize' AND target.[ClaimName] = source.[ClaimName]
                WHEN MATCHED THEN
                    UPDATE SET [Ad] = source.[Ad], [Aciklama] = source.[Aciklama], [KategoriID] = source.[KategoriID],
                               [PolicyName] = source.[ClaimName], [IsActive] = 1, [IsDelete] = 0, [UpdateDate] = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT ([Ad], [Aciklama], [KategoriID], [PolicyName], [ClaimType], [ClaimName], [IsActive], [IsDelete], [CreateDate])
                    VALUES (source.[Ad], source.[Aciklama], source.[KategoriID], source.[ClaimName], N'Authorize', source.[ClaimName], 1, 0, GETDATE());
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [Yetkilendirme]
                WHERE [ClaimType] = N'Authorize'
                  AND [ClaimName] = N'StokBelgesiSil';
                """);
        }
    }
}
