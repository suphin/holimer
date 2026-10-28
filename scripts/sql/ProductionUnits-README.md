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

`UretimYonetimi/Stoklar`, Excel dışa aktarımı, lot dökümü ve depo sayımı malzemenin ana birimine hesaplama anında dönüşüm uygular. Koli/adet gibi farklı birimler için aktif ürün bazlı doğrudan/ters `PrdUnitConversion` tanımları önceliklidir; tanım yoksa genel tanımlar kullanılır. KG/GRAM ve LITRE/ML standart fiziksel dönüşümleri ve eşdeğer etiketler de desteklenir. Eksik/çelişkili katsayıda eksik bakiye göstermek veya farklı birimleri toplamak yerine işlem engellenir. Geçmiş hareket miktarları ve toplam maliyetler değiştirilmez; son hareket satırları özgün birimini korur.

Bu **çalışma anı dönüşümü** için yeniden derleme/yayımlama yeterlidir; yeni migration veya bu SQL betiğini çalıştırma şartı yoktur. Betik yalnızca eski/yeni **1:1 eşanlamlı ID bağlantılarını** onarır; koli/adet katsayısı uygulamaz. Ayrıntılar `scripts/DepoSayimi-README.md` içindedir. Açık sayım taslaklarının stok referanslarını yenileyip farklarını kontrol edin.

Hammadde sarf, manuel depolar arası transfer ve üretim emrine bağlı rezervasyon/hazırlama/sevk de aynı dönüşümü kullanır. Yeni rezervasyonlar miktarın birimini ve ihtiyaç dönüşüm katsayısını kaydeder. Birimi boş eski rezervasyon doğrulanamıyorsa ilgili lot güvenli biçimde engellenir; miktar tahmin edilmez. Üretim emri uzantısının `20261023120000_AddProductionReservationUnits` migration'ı uygulanmalıdır. Ayrıntılar ve testler `scripts/HammaddeSarf-README.md` içindedir.

## Stok belgesinde satır ve birim düzeltme

Admin kullanıcıları bağımsız devir ve depo transfer belgelerinin detayından gerekçe ve onayla satır silebilir. Silme, satırı ve bağlı stok hareketlerini soft-delete eder, belge toplamını yeniler ve denetim kaydı oluşturur. Son satır için belge silme işlemi kullanılmalıdır. Satınalma, kalite, sayım veya ters kayıt sürecine bağlı satırların silinmesi kaynak süreçten yapılır. Admin, kaynak sürece bağlı aktif belgelerde de hem belge hem tek satır düzenleme ekranından birim değiştirebilir; bu yetki satır silme yetkisinden ayrıdır. İptal edilmiş veya ters kayıtla kapatılmış belgeler düzenlenemez. Düzeltme stok satırına ve bağlı hareketlerine uygulanır; kaynak belgedeki birim tanımları otomatik değiştirilmez.

Detay ekranındaki **Kayıt Düzenle**, yalnızca seçilen satırı açar. Miktar, birim, birim maliyet, lot/seri no, üretim tarihi, SKT ve satır notu düzenlenir. Kayıt sırasında diğer satırlar ve belge başlık bilgileri sunucuda korunur; belge toplamı bütün aktif satırlardan yeniden hesaplanır. Bağlı hareketler ve lot bilgileri güncellenir. Lot numarası/tarihleri aynı lotu kullanan kayıtlara da yansır. Admin yetkisi, düzenleme gerekçesi ve mevcut kaynak süreç/bakiye kontrolleri bu işlemde de uygulanır.

Birim seçimi **yanlış kaydedilmiş birimin düzeltilmesidir**: miktar ve birim fiyat otomatik çevrilmez. Örneğin yanlışlıkla `280 Adet` kaydedilmiş `280 KG` için yalnızca birim düzeltilir; gerçekten `10 KG` yerine `10000 GRAM` gösterilecekse miktar ve gram başına fiyat da kullanıcı tarafından girilir. Malzeme kartının ana birimi ve diğer belgeler değişmez. Bağlı giriş/çıkış hareketleri aynı birime güncellenir. Transfer taslakları ana birimde tutulur.

Düzeltme/silme öncesi lot bakiyesi ve aktif rezervasyonlar ürünün ana birimine dönüştürülerek kontrol edilir. Tanımsız/çelişkili dönüşüm, negatif stok/değer veya rezervasyon açığında tüm işlem geri alınır. Bu ekran değişiklikleri için yeni migration gerekmez.

Düzeltme ve satır silme işlemleri SQL kilidini en fazla 5 saniye bekler (bağlantıda daha kısa bir süre varsa o korunur). Kilit alınamazsa işlem geri alınır; düzenlemede girilen değerler tekrar deneme formunda korunur. Otomatik yeniden kayıt yapılmaz. Kullanıcı isteği iptal ederse iptal normal biçimde sonlandırılır; genel zaman aşımı veya bağlantı iptalinde kaydetme sonucu kesinleşmediğinden önce belge detayının kontrol edilmesi istenir. Geçici kilit süresi ayarı her çıkışta eski değerine döndürülür. Tanılama kayıtları belge kimliği ve işlem adını içerir; iptal ekranındaki hata tek başına SQL kilitlenmesini kanıtlamaz.

Doğrulama: `dotnet run --project scripts/tests/InventoryCorrections/InventoryCorrections.csproj`. Testler geçici bir LocalDB veritabanı oluşturup sonunda kaldırır; canlı bağlantı kullanmaz. `--preview` seçeneği gerçek Razor ekranlarını izole verilerle tarayıcıda kontrol etmek için en fazla 15 dakika açık tutar; `/fixture-stop` ile sonlandırılır.
