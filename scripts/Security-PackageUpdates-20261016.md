# NuGet güvenlik güncellemeleri — 16.10.2026

- `System.Security.Cryptography.Pkcs`: 10.0.13 (Models, Data, Common, Web).
- `System.Security.Cryptography.Xml`: 10.0.13 (Models, Data, Common).
- `SixLabors.ImageSharp`: 4.1.2 (Web; önceki sürüm 3.1.12).

Geçişli bağımlılıklarda eski sürümün seçilmesini önlemek için doğrudan sürüm referansları eklendi. Diğer uygulama/EF paketleri topluca yükseltilmedi. NuGet güvenlik denetimi ve PowerShell hata davranışı kapatılmadı; `NoWarn` eklenmedi. Migration veya canlı veritabanı çalıştırılmadı.

## Çalışma zamanı da güncellenmeli

ASP.NET Core, XML kriptografi bileşenini paylaşımlı framework'ten sağlayabilir. Web projesine gereksiz doğrudan XML referansı eklemek `NU1510` üretir; bu nedenle Web, sınıf kitaplıklarından gelen paketi normal SDK ayıklamasıyla kullanır. Derleme/NuGet denetiminin temiz olması, yüklü sistem runtime'ının güncel olduğu anlamına gelmez.

Bu bilgisayardaki bellek içi imza testi `System.Security.Cryptography.Xml` için `Microsoft.AspNetCore.App/10.0.12` ve ürün sürümü **10.0.12** yüklediğini doğruladı. XML açığının framework tarafında da kapanması için geliştirme ve uygulama sunucusundaki **.NET / ASP.NET Core 10.0.13 veya daha yeni güvenlik sürümü** kurulmalı; IIS kullanılıyorsa uygun Hosting Bundle güncellenmeli. Ardından uygulama yeniden başlatılmalı ve yeni paketlerle tekrar yayımlanmalı. Sistem yazılımı bu çalışma sırasında otomatik kurulmadı veya değiştirilmedi.

Sürümleri `dotnet --list-runtimes` ile kontrol edin. DLL ürün sürümünü kontrol ederken `ProductVersion` kullanılmalıdır; .NET `FileVersion` alanı `10.0.1226.*` gibi farklı bir numaralandırmadır.

## Doğrulama

- NuGet Audit açık Web restore: güvenlik uyarısı yok.
- Web ve yeni gider öneri test projesi derlendi; geçici LocalDB üzerinde migration Up/Down, kategori/hesap onayı, eşzamanlılık ve Razor testleri geçti; test veritabanı kaldırıldı.
- PKCS imza/şifreleme ve XML SHA256 imza roundtrip; ImageSharp PNG/JPEG/resize ve 1×1 TIFF Group3/Group4 roundtrip testleri geçti. Bunlar işlev uyumluluk testleridir; eski paylaşımlı XML runtime'ının yamalandığı iddiası değildir.

Kaynaklar: [PKCS duyurusu](https://github.com/advisories/GHSA-2fmv-rv2c-59x7), [XML duyurusu](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6), [ImageSharp düzeltmesi](https://github.com/advisories/GHSA-j9gm-c75j-xc9q), [NuGet NU1510](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1510).
