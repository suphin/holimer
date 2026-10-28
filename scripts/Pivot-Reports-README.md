# Logo pivot raporları

## Kurulum

1. `Update-Database -Context ApplicationDbContext` çalıştırın. Migration: `20261021120000_AddPivotReports`.
2. Admin olarak **Raporlar → Pivot Raporlar → Rapor Tanımları / Yönetim** (`/PivotRaporlar/Yonetim`) açın.
3. Migration'ın pasif oluşturduğu **Fatura Dökümü** tanımını düzenleyin. Kaynak `LogoConnection`, şema `dbo`, view `fatura_dokumu_100`.
4. **View Alanlarını Getir** ile gerçek sütunları yükleyin. Kullanılabilir alanları, varsayılan satır/sütun/değer ve filtreleri kontrol edin. Raporu aktif işaretleyip kaydedin.
5. **Raporlar → Pivot Raporlar** (`/PivotRaporlar`) üzerinden açıp **Pivotu Hesapla** düğmesini kullanın.

İlk tanım, gerçek bağlantıda SELECT ile doğrulanmış sütun adlarını kullanır. Satır `Fatura Türü`, sütun `Ay`, değer `SatIr Matrahı / Toplam` olarak önerilir. Birden fazla yılı birlikte özetlememek için `Yıl` filtresi ekleyin veya `Yıl` alanını satırlara ekleyin. Başlangıç tanımı otomatik aktif edilmez, gerçek veritabanında migration bu geliştirme sırasında çalıştırılmadı.

## Yetkiler

- `Rapor` claim türü + `PivotRaporlar`: bütün aktif pivot raporlarını açma, alan ve filtre seçme.
- `Rapor` claim türü + `PivotRaporlarYonet`: tanım oluşturma/düzenleme, kaynağı ve kullanılabilir sütunları değiştirme; görüntüleme yetkisini de kapsar.
- Admin tüm ekranlara erişir. Geniş `Rapor` rolü veya modül claim'i tek başına erişim sağlamaz.
- Kullanıcı/rol atamaları otomatik yapılmaz. Tanımlar pasif hale getirilerek hem sayfa hem veri erişimi kapatılır.
- Kullanıcı seçimleri oturumluk görünüm seçimidir; kayıtlı varsayılanları yalnızca yönetim ekranı değiştirir. Aynı kaydın eşzamanlı düzenlenmesi rowversion ile korunur. Yeni rapor formunda `Version` boş olabilir; sürüm veritabanı tarafından oluşturulur. Mevcut raporun düzenlenmesinde geçerli sürüm zorunludur. Bu form doğrulaması düzeltmesi için ek migration gerekmez.

## Hesaplama ve güvenlik

- Logo'ya yalnızca SELECT yapılır. View varlığı/sütunlar `sys.views`, `sys.schemas`, `sys.columns` üzerinden doğrulanır. Serbest SQL ve bağlantı dizesi girişi yoktur. Tablo/synonym/procedure kabul edilmez.
- Yönetim ekranı kullanılabilir alanları sınırlar. Gizli alanlar kullanıcıya metadata/veri olarak gönderilmez. İstek değiştirilse dahi sunucuda yeniden doğrulanır.
- View/sütun adları doğrulanıp SQL identifier olarak escape edilir. Filtre değerleri parametrelenir; hesaplama ve koşul türleri sabit izin listelerindendir.
- Veriler SQL tarafında gruplanır; ham fatura satırları tarayıcıya aktarılmaz. En fazla 3 satır alanı, 1 sütun alanı, 3 değer ve 5 filtre kullanılabilir.
- Aynı alan için birden fazla **Eşittir** koşulu alternatif olarak **VEYA** ile birleştirilir. Farklı alanlar ve diğer koşullar **VE** ile birleşir: `Yıl = 2026 VE (Fatura Türü = Perakende VEYA Fatura Türü = Toptan)`. Aynı alanda alt/üst sınır (`Büyük / Eşit`, `Küçük / Eşit`) ve `İçerir` koşulları VE olarak kalır. Eşitlik alternatifleri ile aynı alanın sınırları da VE ile birleşir. Kayıtlı varsayılan filtreler için de aynı kural uygulanır; migration gerekmez.
- Tarih alanları gün bazında gruplanır/filtrelenir. Gün eşitliği saat içeren kayıtları da kapsar. Sayısal filtrelerde binlik ayırıcı kullanılmaz, ondalık nokta kullanılır.
- Toplam/ortalama/en az/en çok sayısal alanlarda çalışır. Adet, seçilen alanın **NULL olmayan kayıtlarını** sayar; DISTINCT fatura veya ürün sayısı değildir.
- Ortalamaların genel toplamı, alt ortalamaların basit ortalaması değil tüm dolu kaynak kayıtlarına göre ağırlıklı ortalamadır. Negatif değerler korunur; NULL ve boş metin grupları ayrıdır.
- Sayısal kaynaklar `decimal(28,6)` dönüşümüyle hesaplanır. View'ın float alanlarında kaynak hassasiyetinin üzerinde kesinlik varsayılmamalıdır. Sığmayan değerlerde eksik/yanlış toplam yerine hata gösterilir.
- 60 saniye SQL zaman aşımı; 5.000 dolu pivot hücresi, 500 satır grubu, 60 sütun grubu ve 20.000 görüntülenecek değer sınırı vardır. Sınır aşımında **kırpılmış sonuç gösterilmez**; filtre daraltılması istenir.
- View'ın iş kuralları olduğu gibi korunur. İptal/alış/satış/iadeler otomatik yeniden yorumlanmaz. Farklı döviz/birimler kullanıcı tarafından ayrılmalıdır. Tekrarlanan fatura başlığı toplamlarını satır bazında toplamak mükerrer tutar yaratabilir; satır matrahı gibi uygun alanlar seçilmelidir.
- Tüm POST'lar antiforgery ister. JSON/ekranlar no-store; hata mesajları bağlantı bilgisi veya SQL ayrıntısı açmaz. Logo SQL hesabında salt-okunur/yalnızca gereken view'ları görebilen yetkiler ayrıca tavsiye edilir.

## Kontroller

```powershell
dotnet run --project scripts/tests/PivotReports/PivotReports.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false
dotnet run --project scripts/tests/ReportPermissions/ReportPermissions.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false
```

İlk test rastgele isimli LocalDB fixture'ında SQL, toplamlar, izinli alanlar, yönetim kaydı/çakışma, Razor, schema snapshot ve migration'ı kontrol edip fixture'ı siler. `-- --preview` beş dakikalık, yalnızca localhost'a bağlı sentetik önizleme sunar; gerçek uygulama işleri çalışmaz. `-- --metadata` ayrıca gerçek LogoConnection üzerinden yalnızca sütun adı/tipi okur; fatura verisi veya bağlantı sırrı yazdırmaz.
