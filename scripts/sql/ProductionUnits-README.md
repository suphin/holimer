# Üretim birimlerinin eski/yeni kayıtlarını düzeltme

Bu betikler **portalın SQL Server veritabanı** içindir. LogoConnection/Logo tablolarını değiştirmez. Otomatik migration veya uygulama açılışında çalışan işlem değildir.

## Ne düzelir?

`Adet`, `ADET`, `AD`, `PCS` gibi 1:1 eşdeğer birimler tek aktif birim ID'sine bağlanır. Gram, kilogram, litre, mililitre, koli ve paket için de yalnızca kendi eşanlamlıları birleştirilir. **KG → GRAM, LITRE → ML, KOLI → ADET dönüşümü yapılmaz**; miktar, fiyat ve maliyetler aynen kalır.

Malzeme kartları, stok hareketleri, stok belge satırları, reçeteler, üretim planları/emirleri, satınalma satırları, referans maliyetleri ve birim dönüşüm bağlantıları taranır. Soft-delete edilmiş tarihsel satırların UnitId bağlantıları da kapsanır. Lotlar, rezervasyon miktarları, belgeler ve hareketler silinmez. Eski birim kartları silinmeden pasifleşir; hedef kartın adı standart gösterime gelir. Birim kodları benzersiz indeks nedeniyle değiştirilmez.

Kod/isim çelişkileri ve tanınmayan birimler ayrı listelenir, otomatik eşleştirilmez. Aktif hedefi olmayan grup atlanır. Hedef tercih sırası: aktif/silinmemiş kayıtlar arasında standart koda sahip kayıt, sonra en düşük ID. Birimler arasında 1 dışında katsayı veya birleşme sonrası farklı dönüşüm katsayıları varsa uygulama tamamen engellenir.

## Uygulama

1. Portal veritabanının tam yedeğini alın. Önce bir kopyada deneyin.
2. `Normalize-ProductionUnits.sql` dosyasını SSMS'te **portal veritabanı seçiliyken** açın. Betiğin tamamını çalıştırın; `@Apply = 0` varsayılanında sadece önizleme çıkar, kalıcı tablo bile oluşturmaz.
3. Eski/hedef birim ID'lerini, inceleme listelerini, etkilenen tablo/satır sayılarını, beklenen stok bakiyelerini ve engelleyici kontrolleri inceleyin. Özellikle birim adının gerçek anlamını teyit edin.
4. Onaydan sonra portalı ve stok/senkronizasyon/Hangfire yazma işlemlerini durdurun. `@Actor` alanını işlem yapan kişiyle doldurun, `@Apply = 1` yapın ve **betiğin tamamını** yeniden çalıştırın. Uygulama tek transaction içindedir; hata varsa tamamı geri alınır.
5. Sonuçtaki `AppliedRunId` değerini saklayın. `PrdUnitMergeRun`, `PrdUnitMergeReferenceBackup`, `PrdUnitMergeUnitBackup` tabloları geri alma için kalmalıdır. Sonra portalı açın, stok raporu ve belge detaylarını kontrol edin.

Görseldeki hareketlerin neti: `13.505 − 1.505 − 4.000 − 2.940 = 5.060 adet`. Bu mevcut hareketlerin netidir; fiziksel sayımı ayrıca teyit etmek gerekir. Yeni sayım/ek giriş/çıkış hareketi oluşturulmaz.

## Geri alma

`Restore-ProductionUnits.sql` içinde `@RunId` alanına `AppliedRunId` yazın. Önce `@Apply = 0` ile inceleyin. Yazma işlemleri durdurulmuş ve veritabanı yedeklenmişken `@Apply = 1` ile uygulayın. Sonradan değişmiş/silinmiş birim referansları veya birim kartları varsa işlem engellenir; kullanıcı değişiklikleri ezilmez. Geri alma sadece o çalıştırmanın tarihsel referanslarını geri getirir; sonradan eklenen satırları taşımaz.

## Rapor düzeltmesi

`UretimYonetimi/Stoklar`, Excel dışa aktarımı ve lot dökümü eşdeğer birim etiketlerini gruplamadan önce normalleştirir. Hareket detayları da aynı gösterimi kullanır. Bu kod değişikliği için uygulamayı yeniden derleyip yayımlamak gerekir; veritabanı betiği geçmiş ID bağlantılarının asıl onarımıdır. Gerçek farklı birimler raporda ayrı kalır.
