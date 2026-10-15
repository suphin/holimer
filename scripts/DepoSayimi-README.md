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

Bu ekran ürün toplamı sayımıdır. Raf/alan stok yeri tanımı veya lot bazlı sayım değildir; sayım farkının lotlara dağıtımı mevcut akıştaki gibi devam eder. Farklı fiziksel birimler (KG/GRAM gibi) doğrudan toplanmaz; sayım/stok birimleri tutarsızsa işlem engellenir. Aynı birimin eşanlamlıları kabul edilir. Miktar sıfır olabilir; en fazla 6 ondalık ve veritabanının desteklediği miktar aralığı kullanılır.

## Tekrar ve eşzamanlılık

Her ekleme isteğinin benzersiz anahtarı vardır; çift tıklama/tekrar gönderim ikinci giriş yaratmaz. Farklı girişler eski sekmelerden de güvenle eklenebilir çünkü diğer satırların üstüne yazılmaz. Güncelleme, kaldırma, taslak bilgisi ve sonuçlandırma güncel revizyon gerektirir; eski sayfa yeni kayıtları ezemez. SQL Server işlemleri transaction ve taslak/depo kilitleriyle korunur. Tekrar sonuçlandırma aynı stok belgesini döndürür, yeni fark hareketi oluşturmaz.

## Migration ve yayımlama

`20261014081603_AddPersistentWarehouseCounts` migration'ı `PrdWarehouseCountDraft` ve `PrdWarehouseCountEntry` tablolarını, yabancı anahtar ve benzersiz indeksleri ekler. EF snapshot modelle eşleşir. Migration stok miktarlarını, mevcut sayım belgelerini ve hareketlerini değiştirmez.

1. Portal veritabanını yedekleyin. Yayımlama öncesinde eski ekranda açık sayım listelerini ayrıca kaydedin/kopyalayın: eski liste oturumda olduğu için uygulama yeniden başlatılırken kaybolabilir.
2. Standart EF migration güncellemesiyle yeni migration'ı uygulayın ve yeni sürümü yayımlayın. Başka bekleyen migration'lar varsa standart güncelleme onları da uygular. Migration olmadan yeni sayım ekranı açılmamalıdır.
3. Tarayıcıdaki eski sayım sayfalarını yenileyin. Eski oturum listesi hâlâ mevcutsa ilk açılışta eldeki son değerler bir kez kalıcı taslağa aktarılır. Eskiden üzerine yazılmış miktarlar geri getirilemez; bu ürünleri yeniden kontrol edin.

Geliştirme sırasında canlı veritabanına migration uygulanmadı ve canlı stoklar değiştirilmedi. Bellek içi senaryolar, Razor form/arşiv kontrolleri, gerçek SQL Server geçici veritabanında migration ve eşzamanlılık testleri kullanıldı; test veritabanı kaldırıldı.
