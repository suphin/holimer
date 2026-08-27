using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;

namespace Ekomers.Models.Entity;

public class UretimEmriMalzeme : BaseEntity
{
    public int UretimID { get; set; }
    public int MalzemeID { get; set; }
    public int? BirimID { get; set; }
    public int ReceteKalemID { get; set; }
    public decimal ReceteMiktari { get; set; }
    public decimal TeorikMiktar { get; set; }
    public decimal RezerveMiktar { get; set; }
    public decimal SevkEdilenMiktar { get; set; }
    public decimal GercekTuketimMiktari { get; set; }
    public decimal IadeMiktari { get; set; }
    public decimal FireMiktari { get; set; }
    public decimal AciklanamayanFark { get; set; }
    public string? Aciklama { get; set; }
}

public class StokRezervasyon : BaseEntity
{
    public int UretimID { get; set; }
    public int UretimEmriMalzemeID { get; set; }
    public int MalzemeID { get; set; }
    public int DepoID { get; set; }
    public string? LotNumara { get; set; }
    public DateTime? SktTarih { get; set; }
    public decimal RezerveMiktar { get; set; }
    public decimal KullanilanMiktar { get; set; }
    public decimal SerbestBirakilanMiktar { get; set; }
    public StokRezervasyonDurumu Durum { get; set; } = StokRezervasyonDurumu.Aktif;
}

public class DepoHazirlamaEmri : BaseEntity
{
    public int UretimID { get; set; }
    public int KaynakDepoID { get; set; }
    public int HedefDepoID { get; set; }
    public DepoHazirlamaDurumu Durum { get; set; } = DepoHazirlamaDurumu.Bekliyor;
    public string? AtananKullaniciID { get; set; }
    public DateTime? TalepTarihi { get; set; }
    public DateTime? HazirlanmaTarihi { get; set; }
    public DateTime? SevkTarihi { get; set; }
    public DateTime? TeslimTarihi { get; set; }
    public string? Aciklama { get; set; }
}

public class DepoHazirlamaKalem : BaseEntity
{
    public int DepoHazirlamaEmriID { get; set; }
    public int UretimEmriMalzemeID { get; set; }
    public int MalzemeID { get; set; }
    public decimal IstenenMiktar { get; set; }
    public decimal HazirlananMiktar { get; set; }
    public decimal SevkEdilenMiktar { get; set; }
    public decimal EksikMiktar { get; set; }
    public string? Aciklama { get; set; }
}

public class DepoHazirlamaKalemLot : BaseEntity
{
    public int DepoHazirlamaKalemID { get; set; }
    public int? StokRezervasyonID { get; set; }
    public string? LotNumara { get; set; }
    public DateTime? SktTarih { get; set; }
    public decimal Miktar { get; set; }
}
