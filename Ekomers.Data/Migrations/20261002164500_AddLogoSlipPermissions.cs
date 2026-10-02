using Ekomers.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261002164500_AddLogoSlipPermissions")]
public sealed class AddLogoSlipPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @CategoryId int;
            SELECT TOP (1) @CategoryId = [ID]
            FROM [AuthorizationCategory]
            WHERE [Ad] = N'Logo Fiş İşlemleri'
            ORDER BY CASE WHEN [IsDelete] = 1 THEN 1 ELSE 0 END, [ID];

            IF @CategoryId IS NULL
            BEGIN
                INSERT INTO [AuthorizationCategory] ([Ad], [Aciklama], [IsActive], [IsDelete], [CreateDate])
                VALUES (N'Logo Fiş İşlemleri', N'Logo üretimden giriş ve sarf fişi erişim yetkileri', 1, 0, GETDATE());
                SET @CategoryId = CAST(SCOPE_IDENTITY() AS int);
            END
            ELSE
                UPDATE [AuthorizationCategory]
                SET [Aciklama] = N'Logo üretimden giriş ve sarf fişi erişim yetkileri', [IsActive] = 1, [IsDelete] = 0
                WHERE [ID] = @CategoryId;

            MERGE [Yetkilendirme] AS target
            USING (VALUES
                (N'Üretimden Giriş Fişlerini Görüntüleme', N'Logo üretimden giriş fişi listesini ve detaylarını salt okunur görüntüleme', N'LogoUretimGirisiGoruntule'),
                (N'Üretimden Giriş Fişlerini Yönetme', N'Üretimden giriş fişi oluşturma, düzenleme, iptal etme ve Logo''ya gönderme', N'LogoUretimGirisiYonet'),
                (N'Sarf Fişlerini Görüntüleme', N'Logo sarf fişi listesini ve detaylarını salt okunur görüntüleme', N'LogoSarfFisiGoruntule'),
                (N'Sarf Fişlerini Yönetme', N'Sarf fişi oluşturma, düzenleme, iptal etme ve Logo''ya gönderme', N'LogoSarfFisiYonet')
            ) AS source ([Ad], [Aciklama], [ClaimName])
            ON target.[ClaimType] = N'Authorize' AND target.[ClaimName] = source.[ClaimName]
            WHEN MATCHED THEN
                UPDATE SET [Ad] = source.[Ad], [Aciklama] = source.[Aciklama], [KategoriID] = @CategoryId,
                           [PolicyName] = source.[ClaimName], [IsActive] = 1, [IsDelete] = 0, [UpdateDate] = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT ([Ad], [Aciklama], [KategoriID], [PolicyName], [ClaimType], [ClaimName], [IsActive], [IsDelete], [CreateDate])
                VALUES (source.[Ad], source.[Aciklama], @CategoryId, source.[ClaimName], N'Authorize', source.[ClaimName], 1, 0, GETDATE());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM [Yetkilendirme]
            WHERE [ClaimType] = N'Authorize'
              AND [ClaimName] IN
                  (N'LogoUretimGirisiGoruntule', N'LogoUretimGirisiYonet', N'LogoSarfFisiGoruntule', N'LogoSarfFisiYonet');

            DELETE FROM [AuthorizationCategory]
            WHERE [Ad] = N'Logo Fiş İşlemleri'
              AND NOT EXISTS (SELECT 1 FROM [Yetkilendirme] WHERE [KategoriID] = [AuthorizationCategory].[ID]);
            """);
    }
}
