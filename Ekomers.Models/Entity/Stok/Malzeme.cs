using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{

	public class Iade : BaseEntity
	{ 
		public int DepoID { get; set; }
		public int IadeSebepID { get; set; } 
		public int IadeDurumID { get; set; } 
		public DateTime Tarih { get; set; }
		public bool IadeSonuc { get; set; }
		public string? Aciklama { get; set; } 
		public string? Musteri { get; set; } 
		public string? SiparisNo { get; set; } 
		public string? Platform { get; set; } 
		public double? Fiyat { get; set; }

	}
	public class IadeSebep : BaseEntity
	{
		public string Ad { get; set; } 
		public string? Aciklama { get; set; }
	}

	public class MalzemeIade: BaseEntity
	{
		public int MalzemeID { get; set; }
		public int DepoID { get; set; }
		public int HareketTurID { get; set; }
		public double Miktar { get; set; }
		public DateTime Tarih { get; set; }
		public bool GirisCikis { get; set; }
		public string? Aciklama { get; set; }
		public string? MalzemeAciklama { get; set; }
		public double? Fiyat { get; set; }
		public int IadeID { get; set; }

	}
	public class MalzemeStok : BaseEntity
	{
		public int MalzemeID { get; set; }
		public int DepoID { get; set; }
		public int HareketTurID { get; set; }
		public double Miktar { get; set; }
		public DateTime Tarih { get; set; }
        public DateTime SktTarih { get; set; }
        public bool GirisCikis { get; set; }
		public string? Aciklama { get; set; }
		public string? LotNumara { get; set; }
		public string? MalzemeAciklama { get; set; }

	}
	public class Malzeme:BaseEntity
	{
		public string Ad { get; set; }
		public string Kod { get; set; }
		public string? Aciklama { get; set; }
		public string? Marka { get; set; }
		public string? Model { get; set; }
		public string? BarkodNo { get; set; }
		public int GrupID { get; set; }
		public int BirimID { get; set; }
		public int TipID { get; set; }
		public double? KritikMiktar { get; set; }
		public string? Fotograf { get; set; }
		public double? Fiyat { get; set; } 
		public double? Maliyet { get; set; } 
		public double? Kdv { get; set; }
		public double? Indirim { get; set; }
		public int? DovizTur { get; set; }
	}
	public class MalzemeGrup: BaseEntity
	{
		public int? ParentID { get; set; }
		public string Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
	}
	public class MalzemeBirim : BaseEntity
	{
		public string Ad { get; set; }
		public string Kod { get; set; }
		public string Aciklama { get; set; }
	}
	public class MalzemeTipi : BaseEntity
	{
		public string Ad { get; set; }
		public string Kod { get; set; }
		public string Aciklama { get; set; }
	}
	public class MalzemeDepo: BaseEntity
	{
		public string Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
		public string? Adres { get; set; }
		public string? Ozellik { get; set; }
		public int DepartmanID { get; set; } 
	}
	public class MalzemeHareketTur : BaseEntity
	{
		public string Ad { get; set; }
		public string Kod { get; set; } 
		public string Aciklama { get; set; }
		public bool? GirisCikisDurum { get; set; }
	}
	public class MalzemeFiyat : BaseEntity
	{
		public int MalzemeID { get; set; }
		[ForeignKey("MalzemeID")]
		public virtual Malzeme? Malzeme { get; set; }
		public string? Aciklama { get; set; }
		public double? Fiyat { get; set; }
		public double? Maliyet { get; set; }
		public DateTime? Tarih {  get; set; }
		public int? DovizTur { get; set; }
	}
	public class DovizTur:BaseEntity
	{
		public string Ad { get; set; }
	}

	public class MalzemeRecete : BaseEntity
	{
		public int MalzemeID { get; set; }
		[ForeignKey("MalzemeID")]
		public virtual Malzeme? Malzeme { get; set; }
		public string? Aciklama { get; set; }
		public string? Birim { get; set; }
		public double? Fiyat { get; set; }
		public double? Miktar { get; set; }
		public DateTime? Tarih { get; set; }
		public int? DovizTur { get; set; }
	}
}
