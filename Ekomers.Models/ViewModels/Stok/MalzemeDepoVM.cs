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
	public class MalzemeDepoVM : BaseVM
	{
		public string Ad { get; set; }
		public string? Kod { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public string? Adres { get; set; }
		[Display(Name = "Özellik")]
		public string? Ozellik { get; set; }
		[Display(Name = "Departman")]
		public string? Departman { get; set; }
		[Display(Name = "Departman")]
		public int DepartmanID { get; set; }
		public IFormFile? Dosya { get; set; }
		public List<MalzemeDepoVM> MalzemeDepoVMListe { get; set; }
		public List<Departman> DepartmanListe { get; set; }
	}
}
