using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260910130000_AddPurchasingWorkflowPermissions")]
    public partial class AddPurchasingWorkflowPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @PurchasingCategoryId int;
                DECLARE @QualityCategoryId int;

                SELECT TOP (1) @PurchasingCategoryId = [ID]
                FROM [AuthorizationCategory]
                WHERE [Ad] IN (N'Satınalma', N'Satın Alma')
                ORDER BY CASE WHEN [IsDelete] = 1 THEN 1 ELSE 0 END, [ID];

                IF @PurchasingCategoryId IS NULL
                BEGIN
                    INSERT INTO [AuthorizationCategory] ([Ad], [Aciklama], [IsActive], [IsDelete], [CreateDate])
                    VALUES (N'Satınalma', N'Satınalma modülü işlem yetkileri', 1, 0, GETDATE());
                    SET @PurchasingCategoryId = CAST(SCOPE_IDENTITY() AS int);
                END
                ELSE
                    UPDATE [AuthorizationCategory] SET [IsActive] = 1, [IsDelete] = 0 WHERE [ID] = @PurchasingCategoryId;

                SELECT TOP (1) @QualityCategoryId = [ID]
                FROM [AuthorizationCategory]
                WHERE [Ad] = N'Kalite'
                ORDER BY CASE WHEN [IsDelete] = 1 THEN 1 ELSE 0 END, [ID];

                IF @QualityCategoryId IS NULL
                BEGIN
                    INSERT INTO [AuthorizationCategory] ([Ad], [Aciklama], [IsActive], [IsDelete], [CreateDate])
                    VALUES (N'Kalite', N'Kalite modülü işlem yetkileri', 1, 0, GETDATE());
                    SET @QualityCategoryId = CAST(SCOPE_IDENTITY() AS int);
                END
                ELSE
                    UPDATE [AuthorizationCategory] SET [IsActive] = 1, [IsDelete] = 0 WHERE [ID] = @QualityCategoryId;

                MERGE [Yetkilendirme] AS target
                USING (VALUES
                    (N'Satınalma Sürecini Geri Alma', N'Kalite stok sonucu ve mal kabul karantina girişini kontrollü ters stok kaydıyla geri alma yetkisi', @PurchasingCategoryId, N'SatinalmaSurecGeriAl'),
                    (N'Satınalma Kayıtlarını Silme', N'Satınalma talep, teklif, sipariş, mal kabul ve kalite kayıtlarını süreç kuralları dahilinde silme yetkisi', @PurchasingCategoryId, N'SatinalmaKayitSil'),
                    (N'Kalite Kararını Geri Alma', N'Tamamlanmış kalite kararını yeniden değerlendirmeye açma yetkisi', @QualityCategoryId, N'KaliteKarariGeriAl')
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
                  AND [ClaimName] IN (N'SatinalmaSurecGeriAl', N'SatinalmaKayitSil', N'KaliteKarariGeriAl');
                """);
        }
    }
}
