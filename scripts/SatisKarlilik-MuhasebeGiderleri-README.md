# Muhasebe kaynaklı giderler ve kârlılık

## İnceleme (16.10.2026)

Bağlı inceleme ortamında Logo 100/01 için 01.01.2026–16.10.2026 aralığında 4.876 adet 700'lü muhasebe hareketi bulundu; kullanılan fiş türü 4'tür. Yeni SELECT kontrolüyle 4.872 hareket sınıflandırılabilir, 4 hareket korunan kapsam dışı hesaplardan gelmiştir. Bağlı portalda 56 kayıtlı reçete maliyet satırının tamamında hammadde + ambalaj toplamı pozitiftir; ayrı RptProductCostVersion kaydı bulunmamıştır. Bunlar inceleme ortamının anlık bulgularıdır; başka firma/bağlantıya genellenmez.

Muhasebe hesap planında giderlerin yanında bağlantı, yansıtma ve fark hesapları vardır. Sabit/değişken davranış yalnızca hesap kodundan atanmaz; ad/kod tabanlı öneriler de kullanıcı onayı olmadan kaydedilmez. Hesap planı referansı: [GİB 1 Sıra No.lu Muhasebe Sistemi Uygulama Genel Tebliği](https://gib.gov.tr/mevzuat/kanun/444/teblig/10705).

## Maliyet ayrımı

- Ürün maliyeti yalnızca kayıtlı hammadde + ambalaj alanlarıdır. Reçete hesabının RawMaterialUnitCost + PackagingUnitCost toplamı; uygun manuel versiyonun MaterialCost alanı kullanılır. İşçilik, nakliye, genel gider, diğer gider ve senaryo sabit/değişken payları ürün maliyetine eklenmez.
- Eski BulkTotal kayıtları ayrıştırılmamış toplamdır; otomatik malzeme maliyeti sayılmaz. Yeni toplu giriş BulkMaterial olarak hammadde/ambalaj ister. Mevcut kayıtlar değiştirilmez.
- Ayrım alan bazındadır; geçmişte malzeme tutarına gömülmüş işçilik/nakliye/başka giderler geriye dönük otomatik ayrıştırılamaz. Kaynak hesapların kullanıcı tarafından kontrol edilmesi gerekir.

## Kullanım

`/SatisKarlilikGiderleri`, kârlılık raporundaki **Muhasebe Giderleri / Gruplar** bağlantısından açılır.

1. Dönemi seçin. Kaynak, satış raporuyla aynı **Logo 100 / 01**'dir; başka firmanın giderleri karıştırılmaz.
2. Kendi gider gruplarınızı ekleyin veya **Hazır Grupları Oluştur** ile 28 kategori için sabit/değişken seçeneklerini (56 grup) oluşturun. Tür: **Sabit / Değişken / Hariç**. Aşama: **İşletme / Finansman**. Varsayılan gider tutarı veya onaysız hesap eşleştirmesi yoktur.
3. Tutarı olan muhasebe hesaplarından uygun olanları seçip gruba bağlayın. Tam hesap kodu eşleşir; üst hesap seçimi bütün alt hesapları kapsamaz. Başlangıç/bitiş günü belirleyin. Önceki açık sınıflandırma yeni başlangıçtan önce kapanır; yeni kural sonraki başlangıçla sınırlandırılır. Sınırlı tarihli çakışmalar reddedilir.
4. Gruplar ve kurallar düzenlenebilir, arşivlenebilir. Kullanılmış grubun davranışı/aşaması değiştirilmez; yeni grup ve tarihli kural kullanılır. Tarih aralığı düzenlemesi veya kural kaldırılması geçmiş raporları etkileyebilir; UI uyarır ve denetim kaydı tutulur.
5. Hesabın **Hareketler** bağlantısından fiş no, fiş/hareket referansı, tarih, açıklama, borç/alacak ve hariç tutma sebebi incelenebilir. 50 satırlık sayfalama vardır.

Manuel gider **tutarı** giriş alanı yoktur. Portal yalnızca grupları ve eşleştirmeleri saklar; tutarlar Logo'daki muhasebe hareketlerinden her raporda SELECT ile okunur. Satır/fiş iptalleri kontrol edilir. Borç − alacak kullanılır: normal gider düzeltmeleri ve iadeler netleştirilir, sadece borç toplamı alınmaz. Tutarlar GL DEBIT/CREDIT yerel para birimidir; işlem dövizleri birbirine eklenmez.

## Kaynak korumaları ve kapsam

- Tablolar: LG_100_01_EMFLINE + LG_100_01_EMFICHE + LG_100_EMUHACC. Tarih muhasebe fişi tarihidir.
- Bu sürümde eşleştirilebilir 7/A ana hesapları: 720, 730, 740, 750, 760, 770, 780. Hesaplar kendiliğinden sabit/değişken/finansman sayılmaz.
- 710/790 hammadde/malzeme hesapları ürün kaynağıyla çifte sayılmasın diye gider eşleştirmesine kapalıdır.
- Bağlantı/yansıtma/fark ve diğer kapsam dışı hesaplar dahil edilmez. Bir fişte 700/701/711/721/731/741/751/761/771/781/798/799 karşı hesabı varsa fişin gider tarafı da yansıtma transferi kabul edilip hariç tutulur. Karma fiş varsa hareket detayında mutlaka incelenmelidir; gerçek giderlerin bu tür fişlerde kaydedilmesi otomatik ayrıştırılmaz.
- Fiș türü 4 mevcut ortamda doğrulandığı için kabul edilir. Diğer türler tutara katılmaz ve inceleme bekleyen hareket sayılır; kod/ad tahminiyle açılış/kapanış/özel fişler dahil edilmez.
- 791–797 (7/B gider) hareketleri görülürse bu sürümde 7/A ile otomatik birleştirilmez; inceleme sayılır ve dönem sonucu bloke edilir. Başka hesap/fiş tiplerine geçiş kaynak incelemesiyle geliştirilmelidir.
- Bir hareket bir hesaba ve bir tarihli kurala bağlıdır. Çakışan kural varsa gider iki kez sayılmaz; sınıflandırma bekler. Hareketsiz dönem, sıfır gider olarak onaylanmaz.

## Kârlılık sonucu

1. Net ciro − satılan ürünlerin hammadde/ambalaj maliyeti = ürün malzeme maliyeti sonrası kâr.
2. Önceki sonuç − işletme sabit/değişken giderleri = işletme giderleri sonrası sonuç.
3. Önceki sonuç − finansman sabit/değişken giderleri = finansman sonrası sonuç.

Satış üzerinden marj ve maliyet üzerinden kâr oranı ayrı gösterilir. Eksik maliyet, sınıflandırılmamış hesap, doğrulanmamış fiş/hesap veya muhasebe verisi bulunmaması dönem sonucu gösterimini engeller. Cari/ürün/fiyat durumu filtresi açıkken şirketin tüm dönem giderleri filtrelenmiş cirodan düşülmez; giderler ayrıca gösterilir.

Ürün/müşteri/kanal/temsilci özetleri yalnızca malzeme maliyeti sonrasıdır. Giderleri ürünlere eşit veya ciro bazında dağıtan örtülü kural yoktur. Bu aşamada dağıtım yapılmaz. Sonuç, raporun ürün kapsamı ve seçilmiş muhasebe giderleri için yönetim göstergesidir; tüm şirket satışlarının kapsandığı veya yasal net kâr olduğu varsayılmaz.

## Yetki ve denetim

Okuma: Admin, Yönetici, Rapor. Grup/kural oluşturma, düzenleme ve arşivleme: yalnızca Admin veya Yönetici; bütün POST işlemlerinde antiforgery vardır. Grup adları benzersizdir; aynı hesap ve başlangıç günü için mükerrer kural engellenir. Tarihli işlemler transaction/Serializable ile yapılır. Yapılandırma değişiklikleri kullanıcı/tarih ve önce/sonra bilgileriyle UserActivityLog'a yazılır. Finansal kaynak tablolara INSERT/UPDATE/DELETE yapılmaz.

## Migration / yayımlama

**20261016101321_AddAccountingExpenseClassification** yalnızca iki portal tablosu ekler: RptAccountingExpenseGroup ve RptAccountingExpenseRule. Unique indeksler ve grup FK'sı vardır. Hesap sınıflandırması seed edilmez; şirket adına karar verilmez. Migration, Logo'ya veya mevcut maliyet/stok kayıtlarına dokunmaz.

1. Portal veritabanını yedekleyin ve bekleyen migration'ları inceleyin.
2. Standart migration/yayımlama sürecinizle yeni migration'ı uygulayın. Bu geliştirme sırasında canlı veritabanına uygulanmadı.
3. Yeni sürümü açıp gider gruplarınızı ve hesap kurallarınızı tanımlayın. Hazır kategoriler ve öneri/onay ekranı için **20261016142324_AddAccountingExpensePresets** migration'ı da gereklidir. İlk açılışta eşleştirmeler yoksa dönem sonucunun kapalı olması beklenen davranıştır.
4. Toplamları muhasebe hareketleriyle mutabık kontrol edin; ardından raporu kullanın. Başka firma/dönem, 7/B veya farklı fiş türü için önce kaynak doğrulaması yapın.

Doğrulama: Web derlemesi, 29 izole maliyet senaryosu, sentetik SQL Server tam rapor/gider testleri; controller rol/CSRF, tarihli CRUD, eşzamanlılık, rollback izleme temizliği, Razor ve gerçek migration Up/Down testleri geçti. Test veritabanları kaldırıldı. Canlı kaynağa yalnızca inceleme SELECT'leri gönderildi, finansal kayıtlar değiştirilmedi.

## Hazır kategoriler ve öneri/onay akışı

- **Hazır Grupları Oluştur** yalnızca Admin/Yönetici onaylı POST işlemidir; Logo'ya yazmaz, hesap kuralı oluşturmaz. Fabrika, operasyon ve finansman alt gruplarını oluşturur. Hammadde/ambalaj kategorileri gider olarak seed edilmez.
- Nullable `PresetKey` ve filtreli unique indeks şablon kimliğini korur. Yeniden çalıştırmak mükerrer grup açmaz; kullanıcı grubun adını değiştirse bile kimlik korunur. Aynı ad ve aynı davranış/aşamaya sahip eski grup şablona bağlanabilir; uyumsuz/pasif grup değiştirilmeden raporlanır. Kullanıcı adları, notları ve kuralları ezilmez.
- **Hesap Önerilerini İncele** (`/SatisKarlilikGiderleri/Oneriler`) seçilen dönemde Logo 100/01 muhasebe hareketi bulunan hesapları tarar; bütün hareketsiz hesap planını taradığı varsayılmaz. Korunan ana hesaplar ve herhangi bir silinmemiş geçmiş/gelecek/pasif kuralı bulunan hesaplar atlanır. Arşivlenmiş kurallar aktif eşleştirme sayılmaz.
- Hesap kodu + Türkçe normalize edilmiş hesap adı birlikte değerlendirilir. Sadece ana koddan sabit/değişken tahmin edilmez. Kira/aidat ve amortisman/sigorta gibi net adlar için gerekçeli sabit önerisi vardır; kullanıcı bunu değiştirebilir. Karma gider adı, personel, fason, nakliye, enerji, Ar-Ge, 740 ve finansmanda inceleme/seçim gerekir. Öneri muhasebe kaydı veya otomatik karar değildir.
- Her sayfa en fazla 100 hesaptır; kod/ad araması ve toplu seçim yalnızca bu sayfayı kapsar. Başlangıçta hiçbir hesap işaretli değildir. Toplu seçim yalnızca görünür ve grubu seçilmiş satırları işaretler; aramada gizlenen satırların seçimi temizlenir.
- Kullanıcı her satırın grubunu değiştirebilir, inceleme gerektiren satıra grup seçebilir. Yalnızca işaretli satırlar, ortak başlangıç/bitiş tarihleri ve açık onayla kaydedilir. Başlangıç varsayılanı seçili dönem başıdır; geçmiş tarihli onay geçmiş yönetim raporlarını etkiler.
- POST, hesapları dönem kaynağından yeniden doğrular; gizli alanları/istemci önerisini güvenilir saymaz. Geçersiz grup, korunan/sahte/mükerrer hesap, 100 satır sınırı ve geçersiz tarihler sunucuda reddedilir. CSRF, Admin/Yönetici yetkisi, audit ve Serializable transaction uygulanır.
- Önizleme sonrası başka işlemde eşleştirilmiş bir hesap sessizce yeniden tarihlendirilmez; değiştirilmeden atlanır. Bu sihirbaz önceki açık dönemleri kapatmaz. Var olan kuralları değiştirmek için eski tarihli düzenleme akışını kullanın.
- Migration yalnızca portal gider grubu tablosuna nullable `PresetKey` ve unique indeks ekler; canlı ortamda çalıştırılmadı, seed/mapping içermez. Down sadece bu kimliği kaldırır, grup/kural/tutar silmez.

Test komutları:

```powershell
node scripts/tests/accounting-expense-suggestions.test.cjs
dotnet run --project scripts/tests/AccountingExpenseSuggestions/AccountingExpenseSuggestions.csproj -p:StaticWebAssetsEnabled=false -- --sqlserver
```

SQL Server testleri yalnızca rastgele adlandırılmış geçici `(localdb)\\MSSQLLocalDB` veritabanında çalışır ve sonunda bunu kaldırır. Gerçek portal/Logo bağlantı ayarları okunmaz.
