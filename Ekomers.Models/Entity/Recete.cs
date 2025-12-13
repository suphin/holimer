using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Recete : BaseEntity
	{ 

		public int UrunID { get; set; } 
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
 
	 
	}

	public class ReceteVM : BaseVM
	{ 

		public int UrunID { get; set; }
		public string? UrunAd { get; set; }
		public string? UrunKod { get; set; }
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		public virtual Malzeme? Malzeme { get; set; }
		public List<ReceteVM> ReceteVMListe { get; set; }
		public List<ReceteParametre> ReceteParametreListe { get; set; }
		public List<ReceteParametreDeger> ReceteParametreDegerListe { get; set; }
		public List<ReceteUrunlerVM> ReceteUrunlerVMListe { get; set; }
	}

	public class ReceteParametre : BaseEntity
	{		 
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }

	}
	public class ReceteParametreDeger : BaseEntity
	{
		public int ReceteID { get; set; }
		public int ParametreID { get; set; } 

	}


	


}
