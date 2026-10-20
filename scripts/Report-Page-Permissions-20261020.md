# Raporlar modülü sayfa yetkileri

Yetki tanımları için Package Manager Console:

```powershell
Update-Database -Context ApplicationDbContext
```

Migration: `20261020120000_AddReportPagePermissions`. Logo veritabanına dokunmaz; portalın Yetkilendirme/AuthorizationCategory tanımlarını ekler. Kullanıcılara/rollere kendiliğinden yetki vermez; mevcut atamaları silmez.

Admin → kullanıcı/rol yetkilendirme ekranında **Raporlar** kategorisinden sayfaları seçin. Kullanıcı yetki değişikliği sonrasında çıkış/giriş yapmalıdır; cookie içindeki eski claim'ler anında yenilenmez.

| Sayfa / işlem | Claim (`Rapor` türünde) |
| --- | --- |
| Bankalar, hesap listesi, hesap ekstresi | Bankalar |
| Banka kredileri, ödeme planı | BankaKredileri |
| Havale / EFT raporu | LogoBankaHareketleri |
| Borç / Alacak, cari hareketleri, eski rapor çalıştır/dışa aktar | BorcAlacak |
| Firma bazlı cari liste/ekstre ve belge detayları | LogoCariEkstre |
| Satış kârlılığı ve rapora bağlı detaylar | SatisKarlilik |
| Ürün maliyetleri, versiyon geçmişi | UrunMaliyetleri |
| Tekli/toplu maliyet girişi ve ürün ön ekleri | UrunMaliyetleriYonet |
| Muhasebe gider grupları ve hesap hareketlerini okuma | MuhasebeGiderleri |
| Gider grubu/hesap kuralı/öneri yönetimi | MuhasebeGiderleriYonet |

Yönetme yetkisi sadece ilgili maliyet sayfasının görüntüleme yetkisini de içerir. Görüntüleme yetkisi yazma/silme yetkisi sağlamaz.

Admin tüm raporlara erişir. `Yönetici`, `Rapor` rolü veya `Modul=Rapor` tek başına rapor sayfalarını açmaz. Kullanıcıya yalnızca gerekli sayfa claim'lerini vermek yeterlidir; fazladan geniş rol atamak gerekmez. Menüler, sayfa içi bağlantılar ve sunucu uç noktaları aynı `ReportPermissions` politikalarını kullanır.

Havale/EFT e-posta planlarını değiştirme, silme, anında gönderme mevcut şekilde yalnızca Admin içindir. Satış kârlılık raporunun kendi dönem gider özeti rapor kapsamındadır; ayrıntılı muhasebe hesap sayfası ayrı izin gerektirir.

Eski Bankalar/BorcAlacak/SatisKarlilik atamaları kendi sayfalarında geçerlidir. Bankalar artık kredi veya Havale/EFT'yi; BorcAlacak ise yeni firma bazlı ekstreyi otomatik açmaz. Bunlar ayrıca seçilmelidir.

Eski `Reports/Run`, `ExportCsv`, `ExportXlsx` yollarında rapor anahtarı da kontrol edilir: Admin olmayanlar yalnızca `BorcAlacak` anahtarını ilgili yetkiyle kullanabilir. Yeni anahtarlar ayrıca açıkça eşlenmeden erişime açılmaz.

Test: `dotnet run --project scripts/tests/ReportPermissions/ReportPermissions.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false -p:OutputPath=$env:TEMP/ekomers-report-permissions-tests`.
