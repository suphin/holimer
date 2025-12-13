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
	 
	public class Sirketler : BaseEntity
	{
		 
		public string? SirketAdi { get; set; }
		public string? SirketYetkili { get; set; }
		public string? SirketYetkiliTel { get; set; }
		public string? SirketYetkiliEmail { get; set; }
		public string? SirketAdres { get; set; }
		public string? SirketVergiDairesi { get; set; }
		public string? SirketVergiNo { get; set; }
		public string? SirketWebSitesi { get; set; }
		public string? SirketLogo { get; set; }
		public string? LogoTigerSirketKodu { get; set; }
		public string? Aciklama { get; set; }
		 
	}

	public class SirketlerVM : BaseVM
	{
		[Display(Name = "Şirket Adı")]
		public string? SirketAdi { get; set; }
		[Display(Name = "Şirket Yetkili")]
		public string? SirketYetkili { get; set; }
		[Display(Name = "Şirket Yetkili Telefon")]
		public string? SirketYetkiliTel { get; set; }
		[Display(Name = "Şirket Yetkili Email")]
		public string? SirketYetkiliEmail { get; set; }
		[Display(Name = "Şirket Adres")]
		public string? SirketAdres { get; set; }
		[Display(Name = "Şirket Vergi Dairesi")]
		public string? SirketVergiDairesi { get; set; }
		[Display(Name = "Şirket Vergi No")]
		public string? SirketVergiNo { get; set; }
		[Display(Name = "Şirket Web Sitesi")]
		public string? SirketWebSitesi { get; set; }
		[Display(Name = "Şirket Logo")]
		public string? SirketLogo { get; set; }
		[Display(Name = "Logo Tiger Şirket Kodu")]
		public string? LogoTigerSirketKodu { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<SirketlerVM> SirketlerVMListe { get; set; }
	}

}
