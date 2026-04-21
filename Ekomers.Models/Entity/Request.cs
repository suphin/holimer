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
	 
	public class Request : BaseEntity
	{
		
		
		
		public int? DurumID { get; set; }
		public int? TurID { get; set; }
		public bool IsDone { get; set; }
		public DateTime TarihSaat { get; set; }=DateTime.Now;
		
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		public int? SirketID { get; set; }
		public bool IsLocked { get; set; } = false;		 
		public DateTime TeslimTarihi { get; set; }		 
		public DateTime RequestDate { get; set; }		 
		public string? TeslimAdres { get; set; }
	}

	public class RequestVM : BaseVM
	{
		

		public int? DurumID { get; set; }
		[Display(Name = "Son Durum")]
		public string? DurumAd { get; set; }
		public string? DurumClass { get; set; }
	
		public bool IsDone { get; set; }
		public DateTime? TarihSaat { get; set; }
		[Display(Name = "Talep Tarihi")]
		public DateTime RequestDate { get; set; }
		public string? Not { get; set; }
 
		 

		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<RequestVM> RequestVMListe { get; set; }
		public List<RequestUrunlerVM> RequestUrunler { get; set; }
		
	
		public int? TurID { get; set; }
		[Display(Name = "Talep Türü")]
		public string? TurAd { get; set; }
		public int? SirketID { get; set; }
		[Display(Name = "Şirket")]
		public string? SirketAd { get; set; }

        public bool IsLocked { get; set; } = false;
	}

	public class RequestDurum : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
		public string? Class { get; set; }
	}
	public class RequestTur : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
 
}
