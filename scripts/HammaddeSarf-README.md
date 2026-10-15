# Hammadde Sarf

Adres: `/HammaddeSarf`. Menü: **Üretim Yönetimi → Hammadde Sarf**.

Bu akış yalnızca portalın `PrdStockMovement` stok bakiyesine etki eder. Logo REST çağrısı yapmaz, Logo sarf fişi oluşturmaz ve üretim emri/rezervasyon/talep kaydı oluşturmaz. Hedef depoya transfer değildir; kaynak lotta tüketim çıkışı oluşturur.

## Kullanım

1. Yeni Hammadde Sarf ekranında kaynak depoyu seçip stokları getirin.
2. Aktif **Hammadde** kartlarının kullanılabilir lotlarını seçin. Aynı lot tek satırda seçilebilir; farklı lotlar ayrı satır olabilir. Miktar seçilen lotun stok birimindedir; birim kullanıcı tarafından değiştirilemez.
3. Tarih, açıklama ve isteğe bağlı satır notlarını girip taslağı kaydedin. Taslak stok düşmez veya rezervasyon yapmaz. Hatalı taslağı iptal ederek yeniden hazırlayabilirsiniz.
4. Detay ekranında **Stoktan Düş** ile işlemi onaylayın. Depo, malzeme, SKT, güncel bakiye/rezervasyon ve lot maliyeti yeniden kontrol edilir. Miktar ve maliyet düşümü aynı transaction içinde kaydedilir; tekrarlanan işlem ikinci kez stok düşmez.
5. Hatalı işlenmiş sarf için yetkili kullanıcı gerekçeli **Sarfı Geri Al** işlemini kullanır. Orijinal kayıt saklanır; aynı lota aynı miktar ve maliyetle ters hareket yazılır. Genel stok belgesi düzenleme/silme ekranından sarf ve ters belgeleri değiştirilemez.

Karantina ve hurda deposu, pasif/silinmiş kart veya lot, süresi geçmiş lot, zorunlu SKT'si eksik lot, negatif maliyet bakiyesi, karışık fiziksel birimler ve rezerve miktarlar kullanılamaz. KG/KILO gibi eşdeğer eski/yeni birimler birlikte değerlendirilebilir; KG/GRAM gibi farklı birimler önce düzeltilmelidir. Birim dönüşümü yapılmaz. Katalogda “Diğer” olarak kalmış hammaddeler için önce kart türünü düzeltin.

Belge/satırlar mevcut stok tablolarında tutulur. Belge türü `RawMaterialConsumption=11`, hareket türü `RawMaterialConsumption=14`, kaynak türü `ManualRawMaterialConsumption` olur. Lot/üretim tarihi/SKT ve notlar sarf satırında korunur; stok hareketi kullanıcı, tarih ve belge referanslarını taşır. Lot maliyeti mevcut lot bakiyesinin ağırlıklı maliyetinden alınır; tam sarfta lot maliyet bakiyesi tamamen düşer.

## Yetki ve yayımlama

- Görüntüleme: Admin veya Üretim modül yetkisi.
- Taslak oluşturma, iptal ve stoktan düşme: Admin veya `HammaddeSarfYonet` işlem yetkisi (Üretim modül yetkisi de gerekir).
- Geri alma: Admin veya `HammaddeSarfGeriAl` işlem yetkisi (Üretim modül yetkisi de gerekir).

`20261012100000_AddRawMaterialConsumptionPermissions` migration'ı iki işlem yetkisini yetki yönetimine ekler; yeni stok tablosu veya kolon eklemez. Kullanıcılara otomatik işlem yetkisi verilmez. Yeni sürümü derleyip yayımlayın; portal veritabanı bağlantısının doğru olduğundan emin olarak standart EF migration güncellemesini uygulayın. Bekleyen başka migration'lar da varsa normal güncellemede onlar da uygulanır. Canlı veritabanı bu geliştirme sırasında değiştirilmemiştir.
