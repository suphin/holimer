using Ekomers.Models.Ekomers; 
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class MalzemeStokVM:BaseVM
	{
		public int MalzemeID { get; set; }
		public string MalzemeAd { get; set; }
		public string MalzemeKod { get; set; }
		[Display(Name = "Açıklama")]
		public string? MalzemeAciklama { get; set; }
		public string? Marka { get; set; }
		public string? Model { get; set; }
		public string? Grup { get; set; }
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
		public int DepoID { get; set; }
		public int DepoTransferID { get; set; }
		[Display(Name = "Depo Ad")]
		public string? DepoAd { get; set; }
		[Display(Name = "Hareket Tür")]
		public int HareketTurID { get; set; }
		[Display(Name = "Hareket Tür")]
		public string? HareketTur { get; set; }
		public bool? HareketTurGirisCikis { get; set; }
		[Display(Name = "Departman Ad")]
		public int DepartmanID { get; set; }
		[Display(Name = "Departman Ad")]
		public string? DepartmanAd { get; set; }
		public double Miktar { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public DateTime Tarih { get; set; }
		public DateTime SktTarih { get; set; }
		public bool GirisCikis { get; set; }
        public bool? GirisCikisDurum { get; set; }
        public IFormFile? Dosya { get; set; }
		public List<DosyaVM>? DosyaListe { get; set; }
		public List<MalzemeStokVM>? MalzemeStokVMListe { get; set; }
		public List<MalzemeGrup>? MalzemeGrupListe { get; set; }
		public List<Departman>? DepartmanListe { get; set; }
		public List<MalzemeHareketTur>? HareketListe { get; set; }
		public double? GirenMiktar { get; set; }
		public double? CikanMiktar { get; set; }
		public double? KalanMiktar { get; set; }
		public double? Fiyat { get; set; }
		public double? Kdv { get; set; }
		public double? Indirim { get; set; }
        public string? LotNumara { get; set; }
        public  string? DovizTurAd { get; set; }


	}
}
