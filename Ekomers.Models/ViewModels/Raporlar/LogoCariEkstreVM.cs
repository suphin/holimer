namespace Ekomers.Models.ViewModels.Raporlar;

public sealed class LogoCariFirmaVM
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public int LocalCurrency { get; set; }
}
public sealed class LogoCariDonemVM
{
    public int Number { get; set; }
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
}
public sealed class LogoCariScopeVM
{
    public LogoCariFirmaVM Company { get; set; } = new();
    public LogoCariDonemVM Period { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Currency => LogoCariLabels.Currency(Company.LocalCurrency);
}
public sealed class LogoCariRaporVM
{
    public List<LogoCariFirmaVM> Companies { get; set; } = [];
    public List<LogoCariDonemVM> Periods { get; set; } = [];
    public LogoCariScopeVM? Scope { get; set; }
    public int FirmNumber { get; set; }
    public int PeriodNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Search { get; set; } = "";
    public bool IncludePassive { get; set; }
    public bool OnlyBalance { get; set; }
    public int Page { get; set; } = 1;
    public long TotalCount { get; set; }
    public long TotalPages => Math.Max(1, (TotalCount + 49) / 50);
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
    public string? Error { get; set; }
    public List<LogoCariHesapVM> Rows { get; set; } = [];
}
public class LogoCariHesapVM
{
    public int ClientRef { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string TaxNumber { get; set; } = "";
    public string TaxOffice { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal DebitBalance => Math.Max(ClosingBalance, 0);
    public decimal CreditBalance => Math.Max(-ClosingBalance, 0);
}
public sealed class LogoCariEkstreVM
{
    public LogoCariScopeVM Scope { get; set; } = new();
    public LogoCariHesapVM Client { get; set; } = new();
    public int Page { get; set; } = 1;
    public long TotalCount { get; set; }
    public long TotalPages => Math.Max(1, (TotalCount + 49) / 50);
    public decimal OpeningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ClosingBalance => OpeningBalance + Debit - Credit;
    public List<LogoCariHareketVM> Rows { get; set; } = [];
}
public class LogoCariHareketVM
{
    public int MovementRef { get; set; }
    public int ClientRef { get; set; }
    public int SourceRef { get; set; }
    public int Module { get; set; }
    public int TransactionCode { get; set; }
    public DateTime Date { get; set; }
    public int Time { get; set; }
    public string Number { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public int CurrencyType { get; set; }
    public decimal CurrencyAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string Kind => LogoCariLabels.Movement(Module, TransactionCode);
}
public sealed class LogoCariBelgeVM
{
    public LogoCariScopeVM Scope { get; set; } = new();
    public LogoCariHesapVM Client { get; set; } = new();
    public LogoCariHareketVM Movement { get; set; } = new();
    public string Kind { get; set; } = "Hareket özeti";
    public string Number { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public DateTime? Date { get; set; }
    public string Description { get; set; } = "";
    public decimal? NetTotal { get; set; }
    public decimal? VatTotal { get; set; }
    public decimal? DiscountTotal { get; set; }
    public bool? Cancelled { get; set; }
    public string? Notice { get; set; }
    public string LineMode { get; set; } = "generic";
    public int Page { get; set; } = 1;
    public long TotalCount { get; set; }
    public long TotalPages => Math.Max(1, (TotalCount + 49) / 50);
    public List<LogoCariBelgeSatirVM> Rows { get; set; } = [];
}
public class LogoCariBelgeSatirVM
{
    public int Ref { get; set; }
    public int LineNumber { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int LineType { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal VatAmount { get; set; }
    public bool VatIncluded { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int CurrencyType { get; set; }
    public decimal CurrencyAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public DateTime? DueDate { get; set; }
    public string SerialNumber { get; set; } = "";
    public string Direction { get; set; } = "";
    public bool Cancelled { get; set; }
}
public static class LogoCariLabels
{
    public static string Currency(int value) => value switch { 0 or 160 => "TRY", 1 => "USD", 20 => "EUR", 17 => "GBP", _ => "Logo döviz " + value };
    public static string Movement(int module, int code) => module switch
    {
        4 => "Fatura", 7 => code switch { 20 => "Gelen havale / EFT", 21 => "Giden havale / EFT", _ => "Banka hareketi" },
        5 => code switch { 3 => "Borç dekontu", 4 => "Alacak dekontu", 5 => "Virman", 6 => "Kur farkı", 14 => "Devir", _ => "Cari hesap fişi" },
        6 => "Çek / senet bordrosu", 10 => "Kasa hareketi", _ => "Modül " + module + " / işlem " + code
    };
    public static string InvoiceLine(int value) => value switch { 0 => "Malzeme", 1 => "Promosyon", 2 => "İskonto", 3 => "Masraf", 4 => "Hizmet", 5 => "Depozito", 6 => "Sabit kıymet", _ => "Satır türü " + value };
}
