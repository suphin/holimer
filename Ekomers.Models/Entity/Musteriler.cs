using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Musteriler:BaseEntity
	{

		public string? LOGICALREF { get; set; }
	 
		
		public string AdSoyad { get; set; } = "";
		public string? Ulke { get; set; }
		public string? Sehir { get; set; }
		public string? Ilce { get; set; }
		public string? Mahalle { get; set; }
		public int? SehirID { get; set; }
		public int? IlceID { get; set; }
		public int? MahalleID { get; set; }
		public string? PostaKod { get; set; }
		public string? Telefon { get; set; }
		public string? Eposta { get; set; }
		public string? Adres { get; set; }
		public string? BizimHesapNo { get; set; }      // “Bizim Hesap No …”
		public int? ParaBirimiID { get; set; }
		public string? Aciklama { get; set; } = string.Empty;
		public string? Not { get; set; }               // 2. satırdaki küçük gri not
		public DateTime? KayitTarihi { get; set; }     // “Kayıt: …”
		public string? KayitEden { get; set; }

		public int TipID { get; set; }    // “Müşteri” | “Potansiyel Müşteri”
		public bool IsKurum { get; set; }              // ikon: kişi/kurum
		public string? KucukResimUrl { get; set; }

		public string? VergiNo { get; set; }
		public string? VergiDairesi { get; set; }
		public string? SirketUnvan { get; set; }


	}
	
	public class MusterilerVM : BaseVM
	{
		public string? LOGICALREF { get; set; }
		[Display(Name ="Ad Soyad")]
		public string AdSoyad { get; set; } = "";
		[Display(Name = "Ülke")]
		public string? Ulke { get; set; }
		[Display(Name = "Şehir Seçiniz")]
		public string? Sehir { get; set; }
		[Display(Name ="İlçe Seçiniz")]
		public string? Ilce { get; set; }
		[Display(Name = "Mahalle Seçiniz")]
		public string? Mahalle { get; set; }
		public int? SehirID { get; set; }
		public int? IlceID { get; set; }
		public int? MahalleID { get; set; }
		[Display(Name = "Posta Kodu")]
		public string? PostaKod { get; set; }
		public string? Telefon { get; set; }
		public string? Eposta { get; set; }
		public string? Adres { get; set; }
		[Display(Name = "Bizim Hesap No")]
		public string? BizimHesapNo { get; set; }      // “Bizim Hesap No …”
		[Display(Name = "Cari Bakiye Tl")]
		public decimal? CariBakiyeTl { get; set; }     // “Cari Bakiye … TL”
		[Display(Name = "Para Birimi")]
		public string ParaBirimi { get; set; } = "TL"; // “Müşteri Para Birimi …”
		public int? ParaBirimiID { get; set; }
		[Display(Name = "Açıklama")]
		public string Aciklama { get; set; } = string.Empty;
		public string? Not { get; set; }               // 2. satırdaki küçük gri not
		public DateTime? KayitTarihi { get; set; }     // “Kayıt: …”
		public string? KayitEden { get; set; }

		public int TipID { get; set; }
		[Display(Name = "Müşteri Tipi")]
		public string? TipAd { get; set; }    // “Müşteri” | “Potansiyel Müşteri”
		public bool IsKurum { get; set; }              // ikon: kişi/kurum
		public List<MusterilerVM> MusterilerVMListe { get; set; }
		public string? KucukResimUrl { get; set; }

		[Display(Name = "Vergi No")]
		public string? VergiNo { get; set; }
		[Display(Name = "Vergi Dairesi")]
		public string? VergiDairesi { get; set; }
		[Display(Name = "Şirket Ünvanı")]
		public string? SirketUnvan { get; set; }
	}


	public class MusteriTip : BaseEntity
	{
		public string Ad { get; set; }
		public string Aciklama { get; set; }
	}
}
