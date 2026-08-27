namespace Ekomers.Models.Enums;

public enum MalzemeKaynakTuru
{
    Belirsiz = 0,
    Logo = 1,
    Portal = 2,
    LogoEslestirildi = 3
}

public enum DepoTuru
{
    Genel = 0,
    Ana = 1,
    Uretim = 2,
    Mamul = 3,
    Karantina = 4
}

public enum ReceteDurumu
{
    Taslak = 0,
    Onayli = 1,
    Pasif = 2
}

public enum UretimEmriDurumu
{
    Taslak = 0,
    Planlandi = 1,
    MalzemeBekliyor = 2,
    DepodaHazirlaniyor = 3,
    UretimeHazir = 4,
    Uretimde = 5,
    Tamamlandi = 6,
    Iptal = 7
}

public enum StokRezervasyonDurumu
{
    Aktif = 0,
    KismenKullanildi = 1,
    Kullanildi = 2,
    SerbestBirakildi = 3,
    Iptal = 4
}

public enum DepoHazirlamaDurumu
{
    Bekliyor = 0,
    Hazirlaniyor = 1,
    EksikMalzeme = 2,
    KismiHazir = 3,
    SevkEdildi = 4,
    TeslimAlindi = 5,
    Iptal = 6
}

public enum StokBelgeTuru
{
    SerbestHareket = 0,
    UretimEmri = 1,
    DepoHazirlama = 2,
    UretimTuketim = 3,
    UretimIade = 4,
    UretimFire = 5,
    MamulGiris = 6,
    Sayim = 7
}
