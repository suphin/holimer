namespace Ekomers.Models.ViewModels;

public sealed class CariHareketDetayVM
{
    public string CariKod { get; set; } = string.Empty;
    public string CariUnvan { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public long TotalCount { get; set; }
    public long TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public decimal BorcToplami { get; set; }
    public decimal AlacakToplami { get; set; }
    public List<CariHareketSatirVM> Rows { get; set; } = [];
}

public sealed class CariHareketSatirVM
{
    public DateTime? FisTarihi { get; set; }
    public string? FisNo { get; set; }
    public string? FisTuru { get; set; }
    public int? FisKod { get; set; }
    public int? Isyeri { get; set; }
    public string? Aciklama1 { get; set; }
    public string? Aciklama2 { get; set; }
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public string? DovizKodu { get; set; }
    public decimal DovizKuru { get; set; }
    public decimal DovizBorc { get; set; }
    public decimal DovizAlacak { get; set; }
}
