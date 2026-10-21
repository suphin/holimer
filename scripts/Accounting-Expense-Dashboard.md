# Yönetici Gider Paneli

Adres: `/SatisKarlilikGiderleri/Panel`. Menü: Raporlar → Yönetici Gider Paneli.

- Mevcut `Rapor=MuhasebeGiderleri` görüntüleme veya `Rapor=MuhasebeGiderleriYonet` yetkisiyle açılır. Admin erişimi korunur. Panel kayıt değiştirmez; yeni yetki/migration gerekmez.
- Kaynak mevcut kârlılık modülüyle aynı Logo **100 / 01** muhasebe kayıtlarıdır. Bu sürüm firma/dönem birleştirme yapmaz. Logo'da yalnızca SELECT kullanılır.
- Aynı `AccountingExpenseService.GetSummaryAsync` ve tarihli, tam hesap kodu eşleştirmesi kullanılır. Sabit/değişken giderin nasıl sayıldığı değiştirilmedi; hesap ve ay kırılımı aynı sınıflandırılmış hareketlerden üretilir.
- Grupların ve ayların toplamı panel net tutarıyla tutarlıdır. Net = borç − alacak; iadeler/düzeltmeler azaltılır, negatif grup/aylar saklanmaz. Hareketsiz aylar grafikte sıfırla tamamlanır. Hareket olup neti sıfır olan grup görünmeye devam eder.
- Tarih (en fazla 36 takvim ayı), kategori, sabit/değişken, grup adı araması ve hareketsiz gruplar filtresi vardır. Gösterge/grafikler filtrelenmiş grupları kapsar; veri kalite uyarıları tüm seçili dönemi kapsar.
- Hazır grupların sabit anahtarı `factory.*` → Fabrika, `operating.*` → Operasyon; finansman aşaması → Finansman. Anahtarsız işletme grubu → Özel işletme grupları. Yeniden adlandırma kategoriyi değiştirmez; isimden tahmin yapılmaz.
- Kategori çubukları pozitif grup tutarlarının payıdır. Negatif gruplar net toplamda kalır ancak yüzdelere eklenmez; kategori neti ile yüzde payının farklı temelleri ekranda açıklanır.
- Açılır gruptaki hesap tutarları sadece o grubun tarihli payıdır. Bir hesap dönem içinde birden fazla gruba bağlandıysa payları ayrılır. “Hesabın Tüm Hareketleri” mevcut hesap hareketleri ekranını yeni sekmede açar; bu ekran hariç/başka gruba ait kayıtları da gösterebilir ve panel bunu açıkça belirtir.
- Hammadde/ambalaj, yansıtma, iptal, kullanıcı tarafından hariç bırakılmış ve inceleme bekleyen kayıtların mevcut hariç tutma kuralları korunur. Kaynak boş veya sınıflandırma eksikse sıfır/kesin gider sonucu varsayılmaz.

Doğrulama:

```powershell
dotnet run --project scripts/tests/AccountingExpenseDashboard/AccountingExpenseDashboard.csproj -p:StaticWebAssetsEnabled=false -p:UseAppHost=false -p:OutputPath=$env:TEMP/ekomers-expense-panel-tests
```

Test yalnızca rastgele adlı yerel LocalDB üzerinde geçici portal/Logo şeması kurar ve sonunda kaldırır; canlı ayarları yüklemez. Tarihli grup değişikliği, negatif düzeltmeler, hariç/sınıflandırılmamış kayıtlar, ay/kategori/hesap mutabakatı, filtreler ve Razor kodlama kontrol edilir. Panel uç noktasının yetkisi `scripts/tests/ReportPermissions` test matrisindedir.
