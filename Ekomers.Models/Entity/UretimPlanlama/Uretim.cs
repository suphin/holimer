using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Uretim : BaseEntity
	{ 
		public DateTime TeslimTarihi { get; set; } 
		public DateTime SiparisTarihi { get; set; }

		public string? PartiNo { get; set; } 
		public DateTime? TerminSuresi { get; set; }

		public int ReceteID { get; set; }
		public int UrunID { get; set; }  
		public int? UreticiID { get; set; }

		public double? HesaplananMiktar { get; set; }
		public double? GerceklesenMiktar { get; set; }
		public double? HmCarpan { get; set; }

		//hesaplamalar
		public double? KayipFireMiktar { get; set; }
		public double? KayipFireOran { get; set; }
		public double? ParasalDeger { get; set; }
		public string? Not { get; set; }
	}
	public class UretimVM : BaseVM
	{
		[Display(Name = "Teslim Tarihi")]
		public DateTime TeslimTarihi { get; set; }
		[Display(Name = "Sipariş Tarihi")]
		public DateTime SiparisTarihi { get; set; }
		[Display(Name = "Sipariş Tarihi ")]
		public DateTime SiparisTarihiBas { get; set; }
		[Display(Name = "Sipariş Tarihi")]
		public DateTime SiparisTarihiSon { get; set; }
		[Display(Name ="Parti No")]
		public string? PartiNo { get; set; }
		[Display(Name = "Termin Süresi")]
		public DateTime? TerminSuresi { get; set; }
		public double? HesaplananMiktar { get; set; }
		public double? GerceklesenMiktar { get; set; }

		public double? KayipFireMiktar { get; set; }
		public double? KayipFireOran { get; set; }
		public double? ParasalDeger { get; set; }
		public double? HmCarpan { get; set; }
		public int ReceteID { get; set; }
		public int UrunID { get; set; }
		[Display(Name = "Ürün Adı")]
		public string? UrunAd { get; set; }
		public int? UreticiID { get; set; }
		[Display(Name = "Üretici Firma")]
		public string? Uretici { get; set; }
		[Display(Name = "Ürün Kodu")]
		public string? UrunKod { get; set; }
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		public string? MalzemelerJson { get; set; }
		public string? tagifyMalzemeler { get; set; }
		public virtual Malzeme? Malzeme { get; set; }
		public List<UretimVM> UretimVMListe { get; set; }
		public List<ReceteParametre> ReceteParametreListe { get; set; }
		public List<ReceteParametreDeger> ReceteParametreDegerListe { get; set; }
		public List<ReceteUrunlerVM> ReceteUrunlerVMListe { get; set; }
		public List<UretimParametreDeger> UretimParametreDeger { get; set; } 
		public List<UretimUrunler> UretimUrunler { get; set; } 
		public List<UretimTeslimat> UretimTeslimatListe { get; set; } 
		public List<UretimUrunlerVM> UretimUrunlerVMListe { get; set; } 
	}
	public class Uretici : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}

	public class UretimUrunler : BaseEntity
	{
		public int UrunID { get; set; }
		public int UretimID { get; set; }

		public double? Deger { get; set; } 
	}
	public class UretimUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public int UretimID { get; set; }

		public double? Deger { get; set; } 
		public string? UrunKod { get; set; }
		public string? UrunAd { get; set; }
		public int ReceteID { get; set; }
		public string? BirimAd { get; set; }
		public int? BirimID { get; set; }
		public string? TipAd { get; set; }
		public int? TipID { get; set; }
		public double? Miktar { get; set; }
		public double? Fiyat { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public string? Aciklama { get; set; }
		public double? Kdv { get; set; }
		public double? Iskonto { get; set; }
		public List<UretimUrunlerVM> UretimUrunlerVMListe { get; set; }
		public string? Grup { get; set; }
		public string? GrupKod { get; set; }
		[Display(Name = "Alt Grup")]
		public string? AltGrup { get; set; }
		[Display(Name = "Alt Grup")]
		public int? AltGrupID { get; set; }
		[Display(Name = "Grup")]
		public int GrupID { get; set; }
	}
	public class UretimParametreDeger: BaseEntity
	{
		public int ParametreID { get; set; }
		public int UretimID { get; set; }
		public double? Deger { get; set; }
		public string? ParametreAd { get; set; }
	}

	public class UretimParametreDegerVM
	{
		public int ParametreId { get; set; }
		public string Deger { get; set; }
	}

	public class UretimTeslimat : BaseEntity
	{ 
		public int UretimID { get; set; }
		public double Miktar { get; set; } 
		public DateTime Tarih { get; set; }
	}
}
