# Hammadde Sarf

## Müşteriye ait fason emanet hammaddeler (26.10.2026)

Menü: **Üretim Yönetimi → Fason Emanet Hammaddeler**, adres: `/FasonHammadde`.

Bu giriş ve stok takibi, her müşteri firma için tek bir ayrı fason depo açar. Firma kodu/unvanı **Firmalar / Depolar → Yeni Firma / Depo** ekranından tanımlanır; depo kodu otomatik üretilir. Aynı firma koduyla ikinci depo oluşturulmaz, giriş belgelerinin firma/depo sahipliği sonradan değiştirilmez. Firma kartları bu aşamada bu ekranda tanımlanır; Logo cari listesi otomatik aktarılmaz.

**Yeni Giriş** ekranında firma/depo, giriş tarihi, irsaliye/teslim belge no, ürün, girilen miktar/birim, lot/seri, üretim tarihi, SKT ve not girilir. Hammadde ve ambalaj kartları ortak katalogdan seçilir. Ürün bazlı birim dönüşümleriyle ana birim hesaplanır; orijinal miktar/birim saklanır. Birim seçilmezse kartın ana birimi kullanılır. Kartta zorunlu lot/SKT kontrol edilir. Taslak stok artırmaz; **Firma Stoğuna Al** onayı gerekir. Aynı belgenin tekrar veya eşzamanlı onayı ikinci hareket oluşturmaz. Hatalı taslak iptal edilebilir; işlenmiş giriş gerekçeli **Girişi Geri Al** ile ters hareket oluşturularak düzeltilir. Orijinal belge ve geçmiş korunur.

Stok ve hareketler yalnızca `FsnWarehouse`, `FsnReceipt`, `FsnReceiptLine`, `FsnStockMovement` tablolarındadır. `PrdWarehouse` / `PrdStockMovement` kayıtları oluşturulmaz: müşteri hammaddeleri fabrika stok raporuna, MRP/rezervasyonlara, normal sarfa, depo transferine veya satınalma/ürün maliyetlerine dahil olmaz. **Firma Stokları** bakiye/lot/SKT, **Hareket Geçmişi** belge, kullanıcı, gerekçe ve satır notlarını gösterir. Bu sürüm giriş ve emanet stok takibini kapsar; fason üretim emrine bağlı sarf, müşteriye iade ve ayrı kalite süreci sonraki entegrasyonlardır. Fabrika sarf ekranından müşteri malzemesi tüketilemez.

Yayımlama öncesi `20261026120000_AddFasonConsignmentStock` migration'ını standart `Update-Database -Context ApplicationDbContext` ile uygulayın. Dört yeni fason tablo ve iki işlem yetkisi eklenir; mevcut stok kayıtları taşınmaz veya yeniden sınıflandırılmaz. Görüntüleme mevcut Üretim modül yetkisini kullanır. Admin dışındaki kullanıcılar için ayrıca **Fason Hammadde Yönetimi** (`FasonHammaddeYonet`) ve gerekiyorsa **Fason Hammadde Geri Alma** (`FasonHammaddeGeriAl`) yetkilerini atayın. Migration kullanıcıya otomatik yetki vermez. Canlı veritabanına bu geliştirme sırasında migration uygulanmadı.

Doğrulama: `dotnet run --project scripts/tests/FasonStock/FasonStock.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false`. Rastgele isimli geçici LocalDB'de migration, firma ayrımı, fabrika stoklarının korunması, birim dönüşümü, onay/geri alma eşzamanlılığı, MVC formları ve CSRF kontrolü çalışır; veritabanı sonunda kaldırılır. `-- --preview` eklenirse konsolda çıkan localhost adresinde aynı geçici verilerle gerçek formlar açılır; `/fixture-stop` sunucuyu kapatır ve temizliği tamamlar.

## Fabrika Hammadde Sarfı

Adres: `/HammaddeSarf`. Menü: **Üretim Yönetimi → Hammadde Sarf**.

Bu akış yalnızca portalın `PrdStockMovement` stok bakiyesine etki eder. Logo REST çağrısı yapmaz, Logo sarf fişi oluşturmaz ve üretim emri/rezervasyon/talep kaydı oluşturmaz. Hedef depoya transfer değildir; kaynak lotta tüketim çıkışı oluşturur.

## Kullanım

1. Yeni Hammadde Sarf ekranında kaynak depoyu seçip stokları getirin.
2. Aktif **Hammadde** kartlarının kullanılabilir lotlarını seçin. Aynı lot tek satırda seçilebilir; farklı lotlar ayrı satır olabilir. Miktar malzemenin **ana stok birimindedir**; birim kullanıcı tarafından değiştirilemez. Eski koli/adet gibi hareketler tanımlı dönüşümle bu birimde birleştirilir.
3. Tarih, açıklama ve isteğe bağlı satır notlarını girip taslağı kaydedin. Taslak stok düşmez veya rezervasyon yapmaz. Hatalı taslağı iptal ederek yeniden hazırlayabilirsiniz.
4. Detay ekranında **Stoktan Düş** ile işlemi onaylayın. Depo, malzeme, SKT, güncel bakiye/rezervasyon ve lot maliyeti yeniden kontrol edilir. Miktar ve maliyet düşümü aynı transaction içinde kaydedilir; tekrarlanan işlem ikinci kez stok düşmez.
5. Hatalı işlenmiş sarf için yetkili kullanıcı gerekçeli **Sarfı Geri Al** işlemini kullanır. Orijinal kayıt saklanır; aynı lota aynı miktar ve maliyetle ters hareket yazılır. Genel stok belgesi düzenleme/silme ekranından sarf ve ters belgeleri değiştirilemez.

Karantina ve hurda deposu, pasif/silinmiş kart veya lot, süresi geçmiş lot, zorunlu SKT'si eksik lot, negatif maliyet bakiyesi ve rezerve miktarlar kullanılamaz. Katalogda “Diğer” olarak kalmış hammaddeler için önce kart türünü düzeltin.

## Aramalı hammadde / lot seçimi (24.10.2026)

Yeni sarf formunda hammadde/lot alanı Select2 ile açılır; kod, ad veya lot numarasıyla arama yapılabilir. Türkçe karakterler için sadeleştirilmiş arama desteklenir (örneğin `yesil sise`). Seçim sonrası kullanılabilir stok ve ana birim güncellenir. Sonradan eklenen satırlar aynı seçimi kullanır; satır kaldırılırken kalan ürün, miktar ve notlar korunur. Doğrulama sonrası artık kullanılamayan seçili lot görünür kalır ve başka lot seçilmesi gerektiği açıklanır. Bu arayüz düzeltmesi için ek migration yoktur.

İzole tarayıcı önizlemesi: test projesini `-- --picker-preview` argümanıyla çalıştırın. Konsolda yazılan localhost `/picker` adresi gerçek Razor formunu ve paketli Select2 dosyalarını yalnızca yapay verilerle açar; veritabanı bağlantısı veya stok kaydı oluşturmaz. `/picker?selected=true` ve `/picker?selected=true&blocked=true` kayıtlı seçim/engelli lot senaryolarıdır. Test formunun gönderimi yalnızca aynı yapay alanları yeniden gösterir. Kod/ad/lot araması, yeni satır seçimi, satır silme sonrası alan adları ve değerlerin korunması, form gönderimi ve kullanılamayan seçimin uyarısı tarayıcıda doğrulandı.

Depolar Arası Transfer (`/ProductionInventory/Yeni`) ürün/lot alanları da kod, ad ve lot numarasıyla aynı arama davranışını kullanır. Arama kutusu seçenek sayısından bağımsız gösterilir; seçili seçenek kullanılabilir stok, ana birim ve maliyet bilgisini taşır. Doğrulama sonrası artık kullanılamayan seçili lot için açıklama gösterilir. Bu arayüz değişikliği ek migration gerektirmez. İzole önizleme adresi `/transfer-picker`; kayıtlı seçim/kullanılamayan lot için aynı `selected` ve `blocked` parametreleri geçerlidir. Form gönderimi yapay alanları yeniden gösterir, gerçek transfer oluşturmaz.

Transferin kaynak ve hedef depo alanları da seçenek sayısından bağımsız aramalıdır. Depo kodu/adı Türkçe karakter kullanılmadan aranabilir, seçim temizlenebilir. Seçimlerin stoklara uygulanması için **Depo Stoklarını Getir** kullanılır. GET formu depo ID'lerini taşır; yüklenen stok formunun gizli depo alanları ile aramalı depo alanlarının HTML ID'leri ayrıdır. İzole önizlemede GET işlemi seçilen kaynak/hedef ID'lerini yeniden gösterir; canlı stok sorgusu yapmaz.

## Stok birimi dönüşümü ve transfer (23.10.2026)

Hammadde Sarf ve `/ProductionInventory` depo transferi, stok raporu/depo sayımı ile aynı `ProductionStockUnitConverter` hesabını ve ortak `ProductionStockLotReader` lot bakiyelerini kullanır. Aktif ürün bazlı doğrudan/ters katsayı genel tanımdan önceliklidir; KG/GRAM ve LITRE/ML fiziksel dönüşümleri ile 1:1 eşanlamlı birimler desteklenir. Kaynak lotta her hareket ayrı ayrı ana birime çevrilir, özgün toplam maliyetler korunarak tek bakiye/maliyet hesaplanır. Geçmiş stok kayıtları değiştirilmez; formda ana birimde gösterilen miktarı girin. Eksik/çelişkili dönüşümler lot bazında açıklanır ve ilgili lot seçilemez; diğer geçerli lotlar kullanılabilir.

Yeni sarf ve transfer satırları ana birimde oluşur. Transferde kaynak çıkışı ve hedef girişi aynı miktar ve toplam maliyettedir; lot, üretim tarihi ve SKT korunur. Stoka işlenirken dönüşümler, güncel serbest bakiye ve maliyet yeniden okunur. İşlem transaction + belge/depo kilitleriyle korunur: bir sarf ile bir transfer aynı stoğu iki kez tüketemez. Transferde depolar ID sırasıyla kilitlenir. Tam lot miktarında özgün kalan maliyetin tamamı kullanılır. Açık taslağın kayıtlı birimi artık ana birime eşdeğer değilse miktar yeniden yorumlanmaz; taslağı iptal edip yeniden hazırlayın.

**Eski rezervasyonların birim alanı boştur.** Bir lotta birimi boş aktif rezervasyon ve ana birim dışındaki fiziksel birimlerde tarihsel hareket birlikte varsa, rezervasyon katsayısı tahmin edilmez; sarf/transfer engellenir. Formda nedeni görünür. İlgili rezervasyonu kaynak üretim sürecinden kontrol edip kaldırın/yeniden hazırlayın. Ana birim/eşanlamlı hareketlerden oluşan lotlarda eski miktar güvenle ana birimde okunabilir. Yeni birimi açık rezervasyonlar, farklı birimli geçmiş hareketler bulunsa da kayıtlı `UnitId` kullanılarak çevrilir; kullanılan/serbest bırakılan miktarlar kendi kayıtlı birimindedir. Eski miktarlara otomatik SQL dönüşümü yapılmaz.

Yalnızca manuel sarf/transfer hesaplaması yeni migration gerektirmez; aşağıdaki üretim emri uzantısı ise rezervasyon metadata migration'ını gerektirir. Mevcut yetkiler değişmez. Testler: `dotnet run --project scripts/tests/StockUnitConversions/StockUnitConversions.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false`. Rastgele isimli, sonunda silinen LocalDB veritabanında sarf/transfer, aynı anda stok düşme, maliyet ve ters kayıt senaryoları çalışır; canlı verilere dokunmaz.

## Üretim emri rezervasyonu ve malzeme sevki

- `DepoGoreviOlustur`, lotları ana birimde toplar. Önce üretim deposundaki stok ayrılır, sonra ana depo stokları FEFO (SKT yoksa lot ID/FIFO) sırasıyla kullanılır. Reçete/ihtiyaç birimi farklı olabilir: örneğin ihtiyaç 2 koli, tanım 1 koli=100 adet ise 200 adet rezerve edilir; ihtiyaç/görev üst satırı 2 koli olarak kalır. Eksik veya çelişkili katsayıda sessizce sıfır ihtiyaç/serbest stok üretilmez.
- Yeni `PrdStockReservation` satırları `UnitId`, `RequirementUnitId`, `RequirementUnitFactor` alanlarını taşır. Rezervasyon miktarı ve ona bağlı `PrdWarehouseTaskLot` miktarları rezervasyonun kayıtlı birimindedir; ihtiyaç sayaçları ihtiyaç biriminde kalır. Görev ekranı iki karşılığı gösterir. Dönüşümün göreli katsayısı veya ihtiyaç birimi rezervasyondan sonra değişirse hazırlama/sevk engellenir; eski miktarlar yeniden yorumlanmaz. Aynı birimin eski/yeni 1:1 eşanlamlıları desteklenir.
- Gerçek hazırlama miktarı artırıldığında gerekli ek rezervasyon da güncellenir; diğer emirlerin rezervasyonları korunur. Artış, serbest fiziksel stoku aşamaz. Hazırlama miktarı azaltılırsa artık tutulmayan miktar serbest kalır ve ihtiyaç rezervasyon sayacı da doğru birime çevrilerek azalır. Kümülatif hazırlama, daha önce sevk edilenin altına inemez.
- Kısmi/tekrarlı sevkte yalnızca henüz sevk edilmemiş fark işlenir. Kaynak rezervasyonun kullanılan miktarı kendi biriminde artar. Üretim deposunda açık birimli yeni rezervasyon oluşur; depo çıkış/giriş hareketleri güncel ana birimde, eşit miktar ve maliyetle yazılır. Lot/SKT/üretim tarihi korunur; maliyet bakiyesi orantılı ve 6 ondalıkla taşınır, son miktar lotun kalan değerini tam alır.
- Üretime çıkış da açık rezervasyon birimini kullanır. Stok hareketi/gerçekleşen miktar ana birimde, verilen/tüketilen/iade/fire ihtiyaç sayaçları ihtiyaç biriminde tutulur. İptal/serbest bırakılmış rezervasyonlar serbest stoku engellemez. Geçmiş stok hareketleri yeniden yazılmaz.
- Rezervasyon, hazırlama, sevk, teslim ve üretime başlatma transaction içinde yürür. Kilit sırası üretim emri → görev → depo ID sırasıdır; manuel sarf/transfer ve sayımla aynı depo kilidi kullanılır. Eşzamanlı iki emir aynı stoğu ayıramaz; rezervasyon ile manuel sarf aynı serbest stoğu iki kez kullanamaz.

### Migration ve yayımlama

`20261023120000_AddProductionReservationUnits` üç **nullable** alan ekler. Eski rezervasyon miktarları, durumları ve geçmiş hareketler değişmez; bugünkü malzeme kartına bakarak eski birim doldurulmaz. Birimi açık olmayan eski kayıtlar yalnızca lot geçmişinin tamamı mevcut ana birime 1:1 eşdeğerse kullanılabilir. Bu doğrulama hazırlamada geçerse ilgili rezervasyona açık birim metadata'sı kaydedilir; farklı fiziksel birimli geçmişte işlem durdurulur.

Portal veritabanını yedekleyin, üretim/stok yazma işlemlerini durdurup `Update-Database -Context ApplicationDbContext` uygulayın ve yeni sürümü yayımlayın. Yeni kod migration uygulanmadan çalıştırılmamalıdır. Bekleyen başka migration'lar da standart güncellemeye dahil olabilir; doğru veritabanını ve migration listesini kontrol edin. Canlı veritabanına bu geliştirme sırasında migration uygulanmadı. Testler migration DDL'sini geçici tabloda, ayrıca ayırma → kısmi sevk → teslim → üretime çıkış akışını ve eşzamanlılık senaryolarını izole LocalDB'de doğrular.

Belge/satırlar mevcut stok tablolarında tutulur. Belge türü `RawMaterialConsumption=11`, hareket türü `RawMaterialConsumption=14`, kaynak türü `ManualRawMaterialConsumption` olur. Lot/üretim tarihi/SKT ve notlar sarf satırında korunur; stok hareketi kullanıcı, tarih ve belge referanslarını taşır. Lot maliyeti mevcut lot bakiyesinin ağırlıklı maliyetinden alınır; tam sarfta lot maliyet bakiyesi tamamen düşer.

## Yetki ve yayımlama

- Görüntüleme: Admin veya Üretim modül yetkisi.
- Taslak oluşturma, iptal ve stoktan düşme: Admin veya `HammaddeSarfYonet` işlem yetkisi (Üretim modül yetkisi de gerekir).
- Geri alma: Admin veya `HammaddeSarfGeriAl` işlem yetkisi (Üretim modül yetkisi de gerekir).

`20261012100000_AddRawMaterialConsumptionPermissions` migration'ı iki işlem yetkisini yetki yönetimine ekler; yeni stok tablosu veya kolon eklemez. Kullanıcılara otomatik işlem yetkisi verilmez. Yeni sürümü derleyip yayımlayın; portal veritabanı bağlantısının doğru olduğundan emin olarak standart EF migration güncellemesini uygulayın. Bekleyen başka migration'lar da varsa normal güncellemede onlar da uygulanır. Canlı veritabanı bu geliştirme sırasında değiştirilmemiştir.
