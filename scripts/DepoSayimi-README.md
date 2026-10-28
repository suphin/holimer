# Kalıcı ve çoklu girişli depo sayımı

Adres `/ProductionStockCount`, menü **Üretim Yönetimi → Depo Sayımı**. Mevcut Admin/Üretim modül yetkisi korunur. Taslaklar kullanıcı kimliği + depo + varsa düzeltilecek sayım belgesi bazında ayrı tutulur; başka kullanıcının taslağı görülemez/değiştirilemez.

## Yeni kullanım

- Aynı ürüne her ekleme ayrı bir giriş oluşturur. Miktar o raf/alanın sayılan miktarıdır, ürünün yeni genel toplamı değildir. Raf/alan ve giriş notu isteğe bağlıdır. Örneğin 20 + 15 + 10 kg ana listede **45 kg** gösterir.
- Ürün altında her girişin miktarı, alanı, notu, kullanıcı ve tarihi görünür. **Güncelle** yalnızca o girişi değiştirir; **Kaldır** yalnızca o girişi toplamdan çıkarır. **Ürünü Kaldır** o ürünün bütün girişlerini kaldırır. Kaldırılan kayıtlar soft-delete ve denetim kaydında saklanır.
- Sayım tarihi/notu **Taslak Bilgilerini Kaydet** ile saklanabilir. Alt satır değişikliklerini kendi **Güncelle** düğmesiyle kaydedin. Kaydedilmeyen değişiklikler için sayfadan ayrılma uyarısı vardır.
- Taslak ve girişler veritabanındadır; oturum kapansa veya sunucu yeniden başlasa da kaybolmaz. **Taslağı İptal Et / Yeni Sayım** önceki taslağı geçmişte saklayıp yeni taslak açar. Son 10 kayıt kendi girişleriyle incelenebilir; tamamlanan/iptal edilen kayıtlar salt okunurdur.
- Ekleme/güncelleme/kaldırma stok hareketi oluşturmaz. **Sayımı Stoka İşle**, yalnızca sayılmış ürünlerin kaydedilmiş giriş toplamını sistem stoklarıyla karşılaştırır. İstemcinin gönderdiği toplamlar kullanılmaz. Fark yoksa taslak tamamlanır, stok belgesi oluşmaz.
- Sayım sırasında sistem stoku değişirse işlem engellenir ve girişler korunur. **Sistem Stok Referanslarını Yenile** sayım miktarlarını değiştirmeden sistem referanslarını günceller. Farkları kontrol edip sonuçlandırın; arada gelen/giden malları fiziksel sayımınıza dahil edip etmediğinizi değerlendirin.
- Düzeltme ekranında önceki belgedeki ürünler başlangıçta **Sayılmadı** görünür. Yalnızca yeni sayım girişi yapılan ürünler işlenir; mevcut stok yanlışlıkla ek bir sayım girişi olarak toplanmaz. Önceki belge değiştirilmez.

Bu ekran ürün toplamı sayımıdır. Raf/alan stok yeri tanımı veya lot bazlı sayım değildir; sayım farkının lotlara dağıtımı mevcut akıştaki gibi devam eder. Miktar sıfır olabilir; en fazla 6 ondalık ve veritabanının desteklediği miktar aralığı kullanılır.

## Stok birimi dönüşümü (23.10.2026)

- Stok raporu, lot bakiyeleri, Excel ve depo sayımı ortak `ProductionStockUnitConverter` ile **malzeme kartındaki ana birimde** hesaplanır. Koli/adet gibi farklı birimlerdeki geçmiş hareketler `PrdUnitConversion` katsayılarıyla çevrilip toplanır. Sayım giriş miktarı ana birimde girilir; sayım fark hareketleri de ana birimde oluşur.
- Aktif, silinmemiş ürün bazlı doğrudan **veya ters** dönüşüm genel dönüşümden önceliklidir. Ürün bazlı tanım yoksa genel doğrudan/ters tanım kullanılır. Zincirleme dönüşüm uygulanmaz: hareket biriminden ana birime bir çift tanımlayın. Aynı fiziksel birimin eşanlamlıları 1:1; KG/GRAM ve LITRE/ML standart fiziksel katsayılarıyla çevrilir. Birden fazla çelişkili aktif katsayı, eksik tanım, geçersiz katsayı veya hassasiyet/aralık aşımı açıklayıcı hata verir; keyfi bir tanım seçilmez.
- Tarihsel hareketlerin miktarı, UnitId'si ve **TotalCost** değeri değiştirilmez. Rapor birim maliyeti korunmuş toplam değerin dönüştürülmüş bakiyeye bölümüdür. Sayım lot dağıtımı ve maliyet hesabı da dönüştürülmüş miktarlarla yapılır; geçmiş birim maliyetinden alınan yedek maliyet ters katsayıyla ölçeklenir. Miktarı sıfır ama değeri kalan kayıtlar rapordan gizlenmez.
- Son 10 hareketin başlık toplamı ana birimdedir; hareket satırları denetim için özgün miktar/birim/maliyetini gösterir. Dolayısıyla geçmişteki bir koli hareketinin burada hâlâ koli görünmesi normaldir.
- Açık taslakta dönüşüm tanımı değişince **Sistem Stok Referanslarını Yenile** kullanılıp fark kontrol edilmelidir. Referans yenileme sayılan miktarı değiştirmez. Ana birim fiziksel olarak değişmişse (ör. Adet → KG) kayıtlı sayım değerleri otomatik yorumlanmaz; ürünü taslaktan kaldırıp yeniden sayın. Hatalı dönüşümde girişler korunur, sonuçlandırma engellenir. Bu kod değişikliği yeni migration veya geçmiş stoklara SQL güncellemesi gerektirmez.

Test: `dotnet run --project scripts/tests/StockUnitConversions/StockUnitConversions.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false`. Testler rastgele isimli, işlem sonunda silinen LocalDB veritabanını kullanır; canlı stoklara bağlanmaz.

## Tekrar ve eşzamanlılık

Her ekleme isteğinin benzersiz anahtarı vardır; çift tıklama/tekrar gönderim ikinci giriş yaratmaz. Farklı girişler eski sekmelerden de güvenle eklenebilir çünkü diğer satırların üstüne yazılmaz. Güncelleme, kaldırma, taslak bilgisi ve sonuçlandırma güncel revizyon gerektirir; eski sayfa yeni kayıtları ezemez. SQL Server işlemleri transaction ve taslak/depo kilitleriyle korunur. Tekrar sonuçlandırma aynı stok belgesini döndürür, yeni fark hareketi oluşturmaz.

## Migration ve yayımlama

`20261014081603_AddPersistentWarehouseCounts` migration'ı `PrdWarehouseCountDraft` ve `PrdWarehouseCountEntry` tablolarını, yabancı anahtar ve benzersiz indeksleri ekler. EF snapshot modelle eşleşir. Migration stok miktarlarını, mevcut sayım belgelerini ve hareketlerini değiştirmez.

1. Portal veritabanını yedekleyin. Yayımlama öncesinde eski ekranda açık sayım listelerini ayrıca kaydedin/kopyalayın: eski liste oturumda olduğu için uygulama yeniden başlatılırken kaybolabilir.
2. Standart EF migration güncellemesiyle yeni migration'ı uygulayın ve yeni sürümü yayımlayın. Başka bekleyen migration'lar varsa standart güncelleme onları da uygular. Migration olmadan yeni sayım ekranı açılmamalıdır.
3. Tarayıcıdaki eski sayım sayfalarını yenileyin. Eski oturum listesi hâlâ mevcutsa ilk açılışta eldeki son değerler bir kez kalıcı taslağa aktarılır. Eskiden üzerine yazılmış miktarlar geri getirilemez; bu ürünleri yeniden kontrol edin.

Geliştirme sırasında canlı veritabanına migration uygulanmadı ve canlı stoklar değiştirilmedi. Bellek içi senaryolar, Razor form/arşiv kontrolleri, gerçek SQL Server geçici veritabanında migration ve eşzamanlılık testleri kullanıldı; test veritabanı kaldırıldı.
