# Satış kârlılığı maliyet kaynakları

`/SatisKarlilik` tek maliyet seçim mekanizmasını detay satırları, üst toplamlar ve müşteri/ürün/kanal/temsilci özetlerinde kullanır. Raporlama sırasında Logo ve portal verileri yalnızca okunur; otomatik maliyet kaydı, versiyon oluşturma veya Logo'ya yazma yapılmaz. Muhasebe gider grupları için ayrıca [gider entegrasyonu notlarına](SatisKarlilik-MuhasebeGiderleri-README.md) bakınız; gider sınıflandırma tabloları migration gerektirir.

## Öncelik ve tarih

1. `/UrunMaliyetleri`: Fatura gününü kapsayan, silinmemiş/pasif olmayan Active veya Closed manuel maliyet versiyonu önceliklidir. Yalnızca **MaterialCost** alınır: hammadde + ambalaj. TRY/TL/TRL için kur 1; diğer para birimlerinde kaydedilmiş ExchangeRate uygulanır. TotalUnitCostTry, işçilik, nakliye, genel gider ve diğer alanlar kullanılmaz. Draft versiyonlar kullanılmaz. Daha yeni versiyon varsa geçmiş günler kendi eski versiyonunu kullanır. Açıkça tanımlanmış sıfır manuel malzeme maliyeti kabul edilir.
2. Manuel versiyon yoksa `/ReceteMaliyetleri/KayitliHesaplar`: Fatura gününe kadar kaydedilmiş en son çalışmadaki ürünün **RawMaterialUnitCost + PackagingUnitCost** değeri alınır. TotalUnitCost, sabit/değişken gider payları kullanılmaz. Kayıtlı hammadde ve ambalaj tutarları TRY'dir; üretim miktarına tekrar bölünmez, zaten birim maliyettir.
3. Uygun maliyet bulunamıyorsa maliyet boş ve satır eksik maliyet olarak gösterilir. Gelecekteki hesabın geçmiş satışa uygulanması veya kaydedilmemiş güncel reçetenin hesaplanması yapılmaz.

Tarih karşılaştırması gün bazındadır. Aynı gündeki çalışmalar arasında en son hesaplama zamanı ve kayıt ID'si seçilir. Aynı kayıtlı çalışmada aynı ürün için farklı birim/tutarda birden fazla reçete varsa rastgele seçim yapılmaz; belirsiz maliyet uyarısı verilir. En yeni kaydın birimi/tutarı geçersizse sessizce eski kayda veya başka kaynağa geçilmez.

Kaydedilmiş hesap bir snapshot'tır. Geçmiş hesaplarda bileşen eksikliği bilgisi saklanmadığından bu entegrasyon onların eksiksizliğini geriye dönük doğrulayamaz; kısmi maliyet içeren eski hesaplar kullanılmadan önce kullanıcı tarafından kontrol edilmelidir. Sıfır toplam veya negatif bileşen içeren reçete malzeme maliyeti kullanılmaz. Negatif manuel malzeme maliyeti ya da geçersiz kur da kullanılmaz. Ayrım kaydedilmiş alanlar bazındadır: malzeme tutarının içine önceden gömülmüş nakliye/işçilik otomatik ayrıştırılamaz.

Eski toplu giriş `BulkTotal` kaynağı toplam maliyeti MaterialCost'a yazmıştır. Bu kayıtlar ayrıştırılmış sayılmaz: kayıtlı reçete varsa ona geçilir, yoksa eksik maliyet uyarılır. Yeni toplu ekran açıkça hammadde + ambalaj tutarı ister ve `BulkMaterial` kaynağıyla kaydeder. Eski kayıtlar/veritabanı tutarları otomatik değiştirilmez.

## Eşleştirme ve birimler

- Manuel maliyet Logo malzeme referansıyla eşleşir.
- Reçete satırı, bağlı portal ürününün dolu **LogoCode** alanıyla; bu alan yoksa kaydedilmiş **ProductCode** ile eşleşir. Kod karşılaştırması trim/büyük-küçük harf normalizasyonu yapar. Ad benzerliğiyle eşleştirme yapılmaz.
- Satış miktarı satış biriminde tutulur. Adet/ADET/PCS gibi eşanlamlılar ve KG/GRAM, LITRE/ML standart dönüşümleri desteklenir.
- Koli/adet gibi dönüşümlerde mevcut `PrdUnitConversion` kuralları gerekir. Malzemeye özel kural genel kuraldan önceliklidir. Ters kural da kullanılabilir; çelişen faktörler, boş birimler veya tanımsız dönüşümler eksik maliyet sayılır. Çok adımlı dönüşüm zincirleri uygulanmaz.
- Aynı ürün/gün farklı birimlerde satılmışsa üst toplam miktarları birime göre ayrı gruplanır. İade miktarının işareti korunarak maliyet de ters yönde hesaplanır.
- Dönüşüm kuralları tarihsel versiyon taşımadığından mevcut tanımlı faktör uygulanır. Geçmişte koli içeriği değişmişse eski raporların dönüşüm tanımları ayrıca kontrol edilmelidir.

Satırda kaynak, versiyon/çalışma numarası, kaynak tarihi ve kaynak birimi görünür. Admin kullanıcıları kayıtlı reçete hesabını bağlantıdan açabilir. Var olan rapor ve maliyet sayfası yetkileri değiştirilmez. Kâr hesaplarının mevcut “bilinen maliyet” yaklaşımı korunur; eksik maliyet varsa uyarı devam eder.

## Testler

`dotnet run --project scripts/tests/SalesProfitabilityCosts/SalesProfitabilityCosts.csproj`

29 senaryoyu yalnızca bellek içi SQLite veritabanlarında çalıştırır.

`dotnet run --project scripts/tests/SalesProfitabilityCosts/SalesProfitabilityCosts.csproj -- --sqlserver`

Ek olarak `(localdb)\MSSQLLocalDB` üzerinde benzersiz `CodexSalesCostsTest_<GUID>` veritabanı oluşturur. Sentetik satış view'ı ve maliyetlerle tam raporu doğrular; iade, karışık birimler, özetlerin tutarlılığı ve kaynak sayaçları test edilir. Muhasebe gideri SELECT kaynakları, sabit/değişken/finansman toplamları, tarihli eşleştirme, iptal/yansıtma/malzeme korumaları ve dönem sonucu uyarıları da test edilir. Sonunda yalnızca bu geçici test veritabanı kaldırılır. Canlı bağlantı bilgileri kullanılmaz.
