using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class AuthorizeCrudVM
	{
		public string Controller { get; set; }
		public string Action { get; set; }
		public string IdName { get; set; }
		public string DosyaYolu { get; set; }
		public int ID { get; set; }
		public int ModulID { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }

		public bool DosyaEkle { get; set; }
		public bool HaritaEkle { get; set; }
		public bool AramaYap { get; set; }
		public bool IsMap { get; set; }

	}
}
