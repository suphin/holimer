using Ekomers.Models.Ekomers;
using Ekomers.Models.FilterVM;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class MalzemelerVM:BaseVM
    {
		public string Ad { get; set; }
		public string Kod { get; set; }
		[Display(Name = "Açıklama")]
		[MinLength(3, ErrorMessage = "Açıklama en az 3 karakter olmalıdır.")]
		public string? Aciklama { get; set; }
		public string? MalzemeAciklama { get; set; }
		public string? Marka { get; set; }
		public string? Model { get; set; }
		public string? BarkodNo { get; set; }
		public string? tagifyGruplar { get; set; }
		public string? Grup { get; set; }
		public string? GrupKod { get; set; }
		[Display(Name = "Alt Grup")]
		public string? AltGrup { get; set; }
		[Display(Name = "Alt Grup")]
		public int? AltGrupID { get; set; }
		[Display(Name = "Grup")]
		public int GrupID { get; set; }
		[Display(Name = "Birim Ad")]
		public string? BirimAd { get; set; }
		[Display(Name = "Birim")]
		public int BirimID { get; set; }
		[Display(Name = "Tip Ad")]
		public string? TipAd { get; set; }
		[Display(Name = "Tip")]
		public int TipID { get; set; }
		[Display(Name = "Kritik Miktar")]
		public double? KritikMiktar { get; set; }
		[Display(Name = "Birim Fiyat")]
		public double? Fiyat { get; set; } 
		public double? Maliyet { get; set; } 
		public double? Kdv { get; set; }
		public double? Indirim { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double? Miktar { get; set; }
		public double? GirenMiktar { get; set; }
		public double? CikanMiktar { get; set; }
		public double? KalanMiktar { get; set; }
		public string? DepoAd { get; set; }
		public int? DepoID { get; set; }
		public int? DepartmanID { get; set; }
		public DateTime? Tarih { get; set; }
		public string? Fotograf { get; set; }
		public IFormFile? Dosya { get; set; }
		public List<MalzemelerVM> MalzemelerVMListe { get; set; }
		public List<MalzemeGrup> MalzemeGrupListe { get; set; }
		public List<MalzemeFiyat> MalzemeFiyatListe { get; set; }
		public List<MalzemeBirim> MalzemeBirimListe { get; set; }
		public List<MalzemeTipi> MalzemeTipiListe { get; set; }
		public List<DovizTur> DovizTurListe { get; set; }
        public List<KategoriTreeItem> KategoriTree { get; set; } 
		// ARAMA KRİTERLERİ
		public MalzemelerFilterVM Filter { get; set; } = new();
		public DateTime? SonFiyatGuncellemeTarih { get; set; }
		public DateTime? SonMaliyetGuncellemeTarih { get; set; }
		public double? MaliyetSatis { get; set; }
	}

	public class TagifyDto
	{
		[JsonPropertyName("value")]
		public string Value { get; set; }

		[JsonPropertyName("ad")]
		public string Ad { get; set; }
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("kod")]
		public string Kod { get; set; }
	}
	 
}
