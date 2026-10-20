using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ekomers.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261020120000_AddReportPagePermissions")]
public sealed class AddReportPagePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @CategoryId int;
        SELECT TOP(1) @CategoryId=ID FROM AuthorizationCategory WHERE Ad=N'Raporlar'
        ORDER BY CASE WHEN IsDelete=1 THEN 1 ELSE 0 END,ID;
        IF @CategoryId IS NULL
        BEGIN
            INSERT AuthorizationCategory(Ad,Aciklama,IsActive,IsDelete,CreateDate)
            VALUES(N'Raporlar',N'Rapor sayfaları ve maliyet yönetimi erişim yetkileri',1,0,GETDATE());
            SET @CategoryId=CONVERT(int,SCOPE_IDENTITY());
        END
        ELSE UPDATE AuthorizationCategory SET IsActive=1,IsDelete=0 WHERE ID=@CategoryId;

        MERGE Yetkilendirme AS target USING(VALUES
            (N'Bankaları Görüntüleme',N'Banka listesi, hesaplar ve hesap ekstresi',N'Bankalar'),
            (N'Banka Kredilerini Görüntüleme',N'Banka kredileri ve kredi ödeme planı',N'BankaKredileri'),
            (N'Havale / EFT Raporunu Görüntüleme',N'Logo banka hareketlerini firma ve cari filtreleriyle görüntüleme; plan yönetimi Admin olarak kalır',N'LogoBankaHareketleri'),
            (N'Borç / Alacak Raporunu Görüntüleme',N'Borç alacak raporu, cari hareketleri ve rapor dışa aktarımı',N'BorcAlacak'),
            (N'Firma Bazlı Cari Ekstreyi Görüntüleme',N'Logo firma bazlı cari listesi, ekstre ve kaynak fatura/banka/kasa/çek/senet detayları',N'LogoCariEkstre'),
            (N'Satış Kârlılık Raporunu Görüntüleme',N'Satış kârlılık raporu, kanal dengesi ve rapora bağlı cari detaylar',N'SatisKarlilik'),
            (N'Ürün Maliyetlerini Görüntüleme',N'Ürün maliyet listesi ve versiyon geçmişini salt okunur görüntüleme',N'UrunMaliyetleri'),
            (N'Ürün Maliyetlerini Yönetme',N'Ürün maliyetlerini görüntüleme, tekli/toplu maliyet versiyonu oluşturma ve ürün ön eklerini yönetme',N'UrunMaliyetleriYonet'),
            (N'Muhasebe Giderlerini Görüntüleme',N'Muhasebe gider grupları, hesap eşleştirmeleri ve hesap hareketlerini salt okunur görüntüleme',N'MuhasebeGiderleri'),
            (N'Muhasebe Giderlerini Yönetme',N'Giderleri görüntüleme, grup/kural oluşturma ve düzenleme, önerileri onaylama ve arşivleme',N'MuhasebeGiderleriYonet')
        ) AS source(Ad,Aciklama,ClaimName)
        ON target.ClaimType=N'Rapor' AND target.ClaimName=source.ClaimName
        WHEN MATCHED THEN UPDATE SET Ad=source.Ad,Aciklama=source.Aciklama,KategoriID=@CategoryId,
            PolicyName=N'Rapor.'+source.ClaimName,IsActive=1,IsDelete=0,UpdateDate=GETDATE()
        WHEN NOT MATCHED THEN INSERT(Ad,Aciklama,KategoriID,PolicyName,ClaimType,ClaimName,IsActive,IsDelete,CreateDate)
            VALUES(source.Ad,source.Aciklama,@CategoryId,N'Rapor.'+source.ClaimName,N'Rapor',source.ClaimName,1,0,GETDATE());
        -- Existing user/role claims are preserved. No new page access is granted automatically.
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        -- Preserve pre-existing Bankalar, BorcAlacak and SatisKarlilik permission definitions and claims.
        UPDATE Yetkilendirme SET IsActive=0,IsDelete=1,UpdateDate=GETDATE()
        WHERE ClaimType=N'Rapor' AND ClaimName IN(N'BankaKredileri',N'LogoBankaHareketleri',N'LogoCariEkstre',
            N'UrunMaliyetleri',N'UrunMaliyetleriYonet',N'MuhasebeGiderleri',N'MuhasebeGiderleriYonet');
        """);
}
