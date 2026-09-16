namespace Ekomers.Data.Services;

public static class ProductionUnitNormalizer
{
    public static string CanonicalCode(string? code, string? name = null)
    {
        var canonical = KnownCode(Token(code)) ?? KnownCode(Token(name));
        return canonical ?? (code ?? name ?? string.Empty).Trim().ToUpperInvariant();
    }

    public static string DisplayName(string? code, string? name = null) => CanonicalCode(code, name) switch
    {
        "ADET" => "Adet",
        "GRAM" => "Gram",
        "KG" => "Kilogram",
        "LITRE" => "Litre",
        "ML" => "Mililitre",
        "KOLI" => "Koli",
        "PAKET" => "Paket",
        _ => string.IsNullOrWhiteSpace(name) ? (code ?? string.Empty).Trim() : name.Trim()
    };

    private static string? KnownCode(string token) => token switch
    {
        "AD" or "ADET" or "PCS" or "PIECE" => "ADET",
        "G" or "GR" or "GRM" or "GRAM" => "GRAM",
        "KG" or "KILO" or "KILOGRAM" => "KG",
        "L" or "LT" or "LTR" or "LITRE" or "LITER" => "LITRE",
        "ML" or "MILILITRE" or "MILILITER" or "MILLILITRE" or "MILLILITER" => "ML",
        "KOLI" => "KOLI",
        "PK" or "PKT" or "PAKET" => "PAKET",
        _ => null
    };

    private static string Token(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var upper = value.Trim().ToUpperInvariant().Replace('İ', 'I');
        return string.Concat(upper.Where(char.IsLetterOrDigit));
    }
}
