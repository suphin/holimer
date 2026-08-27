using Ekomers.Models.Enums;

namespace Ekomers.Models.ViewModels;

public sealed class DepoHazirlamaEmriListeVM
{
    public int ID { get; set; }
    public int UretimID { get; set; }
    public string? UretimEmriNo { get; set; }
    public string? UrunKod { get; set; }
    public string? UrunAd { get; set; }
    public string? KaynakDepo { get; set; }
    public string? HedefDepo { get; set; }
    public DepoHazirlamaDurumu Durum { get; set; }
    public DateTime? TalepTarihi { get; set; }
    public DateTime? PlanlananUretimTarihi { get; set; }
    public int KalemSayisi { get; set; }
}
