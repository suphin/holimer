# Logo Cari Ekstre Raporu

Adres: `/LogoCariEkstre`. Menü: Raporlar → Cari Hesaplar → Cari Ekstre · Firma Bazlı.

## Kapsam ve yetki

- Firmalar portal şirket tanımlarından değil, LogoConnection üzerindeki `L_CAPIFIRM` kayıtlarından gelir. Dönemler `L_CAPIPERIOD` üzerinden firmaya göre seçilir.
- Tek firma ve tek dönem raporlanır. Farklı dönemlerin devir kayıtları üst üste eklenmez. Tarihler seçilen dönemin içinde olmalıdır.
- Tüm uç noktalar oturum ve `Rapor=LogoCariEkstre` sayfa yetkisini gerektirir. Admin erişimi korunur; menü görünürlüğü de aynı politikaya bağlıdır. `Rapor=BorcAlacak` sadece eski Borç/Alacak raporunu açar.
- Logo tarafında yalnızca parametreli SELECT çalışır. Tablo adları sınırlandırılmış sayısal firma/dönemden üretilir. Raporun veri tablosu migration'ı yoktur; sayfa yetki tanımları için `20261020120000_AddReportPagePermissions` uygulanmalıdır.

## Hesaplama

- Cari kartlar `LG_XXX_CLCARD`, hareketler `LG_XXX_YY_CLFLINE` üzerinden alınır. İptal hareketleri dahil edilmez.
- `SIGN=0` borç, `SIGN=1` alacak. Yerel para tutarı `AMOUNT`; işlem dövizi tutarı ayrıca gösterilir, yerel bakiyeye tekrar eklenmez.
- Devir: dönem başlangıcından seçilen başlangıç tarihinin öncesine kadar borç eksi alacak. Başlangıç tarihindeki hareketler dönem hareketlerine dahildir.
- Bakiye: devir + dönem borç − dönem alacak. Satır sırası tarih, `FTIME`, `LOGICALREF`; bakiye tüm hareketlerde hesaplanıp sonra sayfalanır.
- Üst toplamlar yalnızca görünen sayfayı değil, tüm filtrelenmiş carileri kapsar. Pozitif borç bakiyeleri ve negatif alacak bakiyeleri ayrı toplanır.
- Cari araması kod, ünvan ve vergi numarasındadır. Pasif cariler ve sadece bakiyeli cariler seçenekleri vardır. Sayfa büyüklüğü 50'dir.

## Ekstre ve belge detayları

Cari satırındaki Detay ekstreyi; ekstre satırındaki Detay ikinci bir pencerede kaynak belgeyi açar. Firma, dönem, cari ve tarih kapsamı her istekte tekrar doğrulanır.

| Hareket | Kaynak bağlantısı | Gösterilen detay |
| --- | --- | --- |
| Fatura (modül 4) | CLFLINE.SOURCEFREF → INVOICE; CLIENTREF doğrulanır | STLINE malzeme/hizmet/iskonto/masraf satırları, miktar, birim fiyat, KDV, net |
| Banka (modül 7) | CLFLINE.SOURCEFREF → BNFLINE.LOGICALREF → BNFLINE.SOURCEFREF → BNFICHE | Fişin seçilen cariye ait banka/hesap satırları; banka yönü ayrıca belirtilir |
| Cari fiş (modül 5) | CLFLINE.SOURCEFREF → CLFICHE | Aynı fişin cari satırları; virman karşı carisi dahil |
| Kasa (modül 10) | CLFLINE.SOURCEFREF → KSLINES; KSLINES.TRANSREF geri bağlantısı doğrulanır | Kasa hareketi, tutar ve döviz |
| Çek/senet (modül 6) | CLFLINE.SOURCEFREF → CSROLL; CARDREF doğrulanır → CSTRANS/CSCARD | Portföy/seri no, banka, vade, tutar, durum |

Diğer modüllerde veya kaynak bağlantısı doğrulanamıyorsa uyarıyla doğrulanmış cari hareket özeti gösterilir; aynı sayısal referanslı farklı belge tahmin edilmez. Fatura ve ekstre tutarı mahsup/iskonto/tevkifat vb. nedeniyle farklı olabilir; kaynak toplamı ayrı etiketlidir.

Detay pencereleri sayfalanır. Yetki/oturum hatası, yönlendirilmiş giriş sayfası veya beklenen partial başlığı olmayan yanıt pencereye eklenmez. Önceki istekler iptal edilir ve geç gelen yanıtın başka cari/firma penceresine yazılması engellenir.

## Doğrulama

```powershell
node scripts/tests/logo-cari-ekstre-modal.test.cjs
dotnet run --project scripts/tests/LogoCustomerStatement/LogoCustomerStatement.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false -p:OutputPath=$env:TEMP/ekomers-cari-ekstre-tests
```

.NET testi yalnızca yerel `(localdb)\MSSQLLocalDB` üzerinde rastgele adlı geçici bir test veritabanı oluşturur ve sonunda kaldırır. Portal/Logo bağlantı ayarlarını yüklemez. Firma/dönem izolasyonu, bakiye ve sayfalama, kaynak belge ilişkileri, yetki, Razor kodlama ve kapsamlı bağlantılar test edilir.

Geliştirme sırasında gerçek Logo şeması ve kaynak referans ilişkileri SELECT ile doğrulandı. Üretim verisine yazılmadı.
