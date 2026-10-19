using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ekomers.Models.Entity.Profitability;

namespace Ekomers.Data.Services.Profitability;

public sealed record AccountingExpensePreset(string Key, string Name, RptExpenseBehavior Behavior, RptExpenseStage Stage);
public sealed record AccountingExpenseSuggestion(string Category, string Reason, string? PresetKey);

/// <summary>Conservative, explainable proposals only. Never persists assignments or infers from a main code alone.</summary>
public static class AccountingExpenseSuggestions
{
    private static readonly (string Key, string Name)[] Categories =
    [
        ("factory.direct-staff", "Fabrika / Direkt işçilik"),
        ("factory.indirect-staff", "Fabrika / Dolaylı personel"),
        ("factory.subcontract", "Fabrika / Fason üretim"),
        ("factory.rent", "Fabrika / Kira ve tesis"),
        ("factory.energy", "Fabrika / Enerji ve su"),
        ("factory.maintenance", "Fabrika / Bakım ve onarım"),
        ("factory.meals", "Fabrika / Yemek ve personel servisi"),
        ("factory.supplies", "Fabrika / Yardımcı sarflar"),
        ("factory.depreciation", "Fabrika / Amortisman ve sigorta"),
        ("factory.vehicle", "Fabrika / Taşıt giderleri"),
        ("factory.logistics", "Fabrika / Tedarik lojistiği"),
        ("factory.other", "Fabrika / Diğer giderler"),
        ("operating.staff", "Operasyon / Yönetim ve personel"),
        ("operating.office", "Operasyon / Ofis ve altyapı"),
        ("operating.professional", "Operasyon / Profesyonel hizmetler"),
        ("operating.sales-staff", "Operasyon / Satış personeli"),
        ("operating.advertising", "Operasyon / Reklam ve ajans"),
        ("operating.events", "Operasyon / Etkinlik ve tanıtım"),
        ("operating.distribution", "Operasyon / Lojistik ve dağıtım"),
        ("operating.representation", "Operasyon / Temsil ve seyahat"),
        ("operating.vehicle", "Operasyon / Taşıt giderleri"),
        ("operating.meals", "Operasyon / Yemek ve personel servisi"),
        ("operating.depreciation", "Operasyon / Amortisman ve sigorta"),
        ("operating.research", "Operasyon / Ar-Ge ve ürün geliştirme"),
        ("operating.other", "Operasyon / Diğer giderler"),
        ("finance.interest", "Finansman / Faiz"),
        ("finance.fees", "Finansman / Komisyon ve masraflar"),
        ("finance.fx", "Finansman / Kur farkları")
    ];

    public static IReadOnlyList<AccountingExpensePreset> Presets { get; } = Categories.SelectMany(c => new[]
    {
        new AccountingExpensePreset(c.Key + ".fixed", c.Name + " / Sabit", RptExpenseBehavior.Fixed,
            c.Key.StartsWith("finance.") ? RptExpenseStage.Financing : RptExpenseStage.Operating),
        new AccountingExpensePreset(c.Key + ".variable", c.Name + " / Değişken", RptExpenseBehavior.Variable,
            c.Key.StartsWith("finance.") ? RptExpenseStage.Financing : RptExpenseStage.Operating)
    }).ToArray();

    public static string Normalize(string value)
    {
        var text = value.ToLower(new CultureInfo("tr-TR")).Normalize(NormalizationForm.FormD);
        text = new string(text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Replace('ı', 'i');
        return Regex.Replace(text, "[^a-z0-9]+", " ").Trim();
    }

    public static AccountingExpenseSuggestion Suggest(string code, string name)
    {
        if (!AccountingExpenseService.CanMap(code)) return new("Kapsam dışı", "Hammadde/ambalaj, yansıtma, fark ve 7/B hesapları otomatik bağlanamaz.", null);
        var main = code[..3];
        var text = Normalize(name);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        bool Has(params string[] options) => options.Any(words.Contains);
        bool Phrase(string phrase) => (" " + text + " ").Contains(" " + phrase + " ");
        AccountingExpenseSuggestion Result(string key, string reason, RptExpenseBehavior? behavior = null) =>
            new(Categories.Single(c => c.Key == key).Name, reason,
                behavior.HasValue ? key + (behavior == RptExpenseBehavior.Fixed ? ".fixed" : ".variable") : null);

        if (main == "740") return new("Hizmet üretimi · inceleme", "740'ın hizmet üretimi niteliği ve giderin hangi bölüme ait olduğu doğrulanmalı.", null);
        if (main == "780")
        {
            var kinds = new List<string>();
            if (Has("faiz", "faizi", "faizleri")) kinds.Add("finance.interest");
            if (Has("komisyon", "komisyonu", "masraf", "masraflari")) kinds.Add("finance.fees");
            if (Has("kur", "kambiyo")) kinds.Add("finance.fx");
            return kinds.Count == 1 ? Result(kinds[0], "Finansman türü hesap adından anlaşılıyor; sabit/değişken türünü borç ve sözleşme koşullarına göre siz seçiniz.")
                : new("Finansman · inceleme", "Finansman alt türü veya sabit/değişken davranış kesin değil; elle grup seçiniz.", null);
        }
        if (main == "750") return Result("operating.research", "Ar-Ge kapsamı önerildi; proje/personel/hizmet niteliğine göre sabit/değişken türünü seçiniz.");

        var vehicle = Has("arac", "araclar", "araclari", "tasit", "tasitlar", "tasitlari", "kasko", "akaryakit", "benzin", "motorin");
        var rent = Has("kira", "kirasi", "kiralar", "kiralari", "kiralama", "aidat", "aidati");
        var energy = Has("elektrik", "elektrigi", "dogalgaz", "su", "enerji");
        var meals = Has("yemek", "yemegi", "yemekhane") || Phrase("personel servisi");
        var maintenance = Has("bakim", "bakimi", "onarim", "onarimi", "tamir");
        var depreciation = Has("amortisman", "amortismanlari", "sigorta", "sigortasi");
        var staff = Has("ucret", "ucretleri", "maas", "maaslar", "maaslari", "personel", "sgk", "ssk", "mesai", "iscilik");
        if (main == "720")
            return Result("factory.direct-staff", "Direkt işçilikte aylık ücret ile fazla mesai/parça başı ödeme ayrılmalı; 720 tek başına değişken anlamına gelmez.");

        var matches = new List<(string Key, string Reason, RptExpenseBehavior? Behavior)>();
        void Add(bool condition, string key, string reason, RptExpenseBehavior? behavior = null)
        { if (condition) matches.Add((key, reason, behavior)); }
        var factory = main == "730";
        if (vehicle)
            return Result(factory ? "factory.vehicle" : "operating.vehicle", "Taşıt kullanım bölümü ve kira/sigorta ile yakıt/bakım ayrımı doğrulanmalı; türü siz seçiniz.");
        Add(rent, factory ? "factory.rent" : main == "770" ? "operating.office" : "operating.other",
            "Kira/aidat ifadesi ve ana hesap birlikte değerlendirildi. Sabit kira varsayımını onaylamadan önce sözleşmeyi kontrol ediniz.", RptExpenseBehavior.Fixed);
        Add(energy, factory ? "factory.energy" : "operating.office", "Enerjide sabit bedel ile tüketim ayrılmalı; tek hesaptaki karma tutar otomatik bölünmez.");
        Add(meals, factory ? "factory.meals" : "operating.meals", "Yemek/serviste sabit sözleşme ile kişi-gün kullanımı ayrılmalı.");
        Add(maintenance, factory ? "factory.maintenance" : "operating.office", "Bakım sözleşmesi ve kullanıma bağlı onarım aynı davranışta olmayabilir.");
        Add(depreciation, factory ? "factory.depreciation" : "operating.depreciation", "Amortisman/sigorta için sabit gider önerisi; poliçe ve amortisman yöntemini kontrol ediniz.", RptExpenseBehavior.Fixed);
        Add(staff && !meals, factory ? "factory.indirect-staff" : main == "760" ? "operating.sales-staff" : "operating.staff", "Maaş, SGK, prim ve fazla mesai aynı davranışta değildir; türü seçiniz.");
        Add(factory && Has("fason", "dolum", "paketleme"), "factory.subcontract", "Fason bedelin malzeme maliyetine gömülü olmadığını ve adet/parti/sözleşme esasını kontrol ediniz.");
        Add(factory && Has("sarf", "sarflar", "sarflari", "temizlik", "koruyucu"), "factory.supplies", "Yardımcı sarf niteliğini ve reçete malzemesiyle çifte sayılmadığını kontrol ediniz.");
        Add(Has("nakliye", "nakliyesi", "kargo", "sevkiyat", "dagitim", "lojistik"), factory ? "factory.logistics" : main == "760" ? "operating.distribution" : "operating.other",
            "Tedarik nakliyesi ile müşteri sevkiyatını ayırınız; malzeme maliyetindeki taşıma tekrar düşülmemeli. Türü siz seçiniz.");
        Add(main == "760" && Has("reklam", "ajans", "tasarim", "tanitim"), "operating.advertising", "Aylık ajans sözleşmesi ve iş/kampanya bazlı hizmet ayrılmalı.");
        Add(main == "760" && Has("etkinlik", "kongre", "fuar", "sponsorluk"), "operating.events", "Organizasyon bazlı gider otomatik olarak satışa bağlı değişken gider sayılmaz.");
        Add(Has("temsil", "agirlama", "seyahat", "konaklama"), "operating.representation", "Temsil/seyahat amacı ve sözleşme koşullarına göre türü seçiniz.");
        Add(main == "770" && Has("danismanlik", "musavirlik", "hukuk", "avukat", "denetim"), "operating.professional", "Aylık danışmanlık ve iş başına hizmet ayrılmalı.");
        Add(main == "770" && Has("internet", "telefon", "yazilim", "lisans", "kirtasiye"), "operating.office", "Abonelik, lisans ve kullanıma bağlı giderlerin davranışını kontrol ediniz.");

        // Multiple cost kinds in one account cannot be assigned/split safely by name.
        if (matches.Count > 1) return new("Birden fazla gider türü · inceleme", "Hesap adı birden fazla kaleme işaret ediyor. Tek grup seçmeden önce alt hesapları veya dağılımı inceleyiniz.", null);
        if (matches.Count == 1)
        {
            var match = matches[0];
            return Result(match.Key, match.Reason, match.Behavior);
        }
        return new("İnceleme gerekli", "Hesap kodu ve adından güvenilir kategori/tür belirlenemedi; elle grup seçiniz.", null);
    }
}
