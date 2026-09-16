using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260915090000_AddGranularPurchasingPermissions")]
    public partial class AddGranularPurchasingPermissions : Migration
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
                    (N'Satınalma Kayıtlarını Görüntüleme', N'Satınalma talep, teklif, sipariş, tedarikçi ve mal kabul kayıtlarını görüntüleme yetkisi', @PurchasingCategoryId, N'SatinalmaGoruntule'),
                    (N'Satınalma Talebi Oluşturma', N'Yeni satınalma talebi oluşturma ve kendi taslağını onaya gönderme yetkisi', @PurchasingCategoryId, N'SatinalmaTalepOlustur'),
                    (N'Satınalma Talebi Düzenleme', N'Satınalma taleplerini düzenleme ve gerektiğinde taslağa döndürme yetkisi', @PurchasingCategoryId, N'SatinalmaTalepDuzenle'),
                    (N'Satınalma Talebi Onaylama', N'Onay bekleyen satınalma talep satırlarını kabul veya reddetme yetkisi', @PurchasingCategoryId, N'TalepKabul'),
                    (N'Tedarikçi Yönetimi', N'Tedarikçi oluşturma, düzenleme ve Portal/Logo aktarımı yapma yetkisi', @PurchasingCategoryId, N'SatinalmaTedarikciYonet'),
                    (N'Teklif Yönetimi', N'Tedarikçi teklifi oluşturma, düzenleme ve onaya gönderme yetkisi', @PurchasingCategoryId, N'SatinalmaTeklifYonet'),
                    (N'Teklif Onaylama', N'Tedarikçi teklif satırlarını kabul veya reddetme yetkisi', @PurchasingCategoryId, N'TeklifKabul'),
                    (N'Satınalma Siparişi Yönetimi', N'Satınalma siparişinin nakliye ve teslimat bilgilerini yönetme yetkisi', @PurchasingCategoryId, N'SatinalmaSiparisYonet'),
                    (N'Mal Kabul Yönetimi', N'Satınalma siparişleri için mal kabul oluşturma ve düzenleme yetkisi', @PurchasingCategoryId, N'SatinalmaMalKabulYonet'),
                    (N'Karantina İşlemleri', N'Mal kabul lotlarını karantinaya alma ve kalite analiz kayıtlarını oluşturma yetkisi', @PurchasingCategoryId, N'SatinalmaKarantinaIsle'),
                    (N'Ek Maliyet Dağıtımı', N'Mal kabul ve lotlara nakliye ile diğer ek maliyetleri dağıtma yetkisi', @PurchasingCategoryId, N'SatinalmaEkMaliyetDagit'),
                    (N'Satınalma Stoğunu Sonuçlandırma', N'Kalite kararı verilen lotları kullanılabilir stok, iade veya hurda olarak sonuçlandırma yetkisi', @PurchasingCategoryId, N'SatinalmaStokSonuclandir'),
                    (N'Kalite Analizlerini Görüntüleme', N'Karantina kalite analizlerini salt okunur görüntüleme yetkisi', @QualityCategoryId, N'KaliteAnalizGoruntule'),
                    (N'Kalite Analizlerini Düzenleme', N'Numune, analiz ve spesifikasyon sonuçlarını düzenleme yetkisi', @QualityCategoryId, N'KaliteAnalizDuzenle'),
                    (N'Kalite Kararı Verme', N'Kalite analizine onay, şartlı onay veya ret kararı verme yetkisi', @QualityCategoryId, N'KaliteKararVer')
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
                  AND [ClaimName] IN (
                    N'SatinalmaGoruntule', N'SatinalmaTalepOlustur', N'SatinalmaTalepDuzenle',
                    N'SatinalmaTedarikciYonet', N'SatinalmaTeklifYonet', N'SatinalmaSiparisYonet',
                    N'SatinalmaMalKabulYonet', N'SatinalmaKarantinaIsle', N'SatinalmaEkMaliyetDagit',
                    N'SatinalmaStokSonuclandir', N'KaliteAnalizGoruntule', N'KaliteAnalizDuzenle', N'KaliteKararVer');
                """);
        }
    }
}
