using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class GruplarVM:BaseVM
	{
		public int? ParentID { get; set; }
		public string Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
		public List<GruplarVM> GruplarVMListe { get; set; }
		public MalzemeGrup ParentGrup { get; set; }
		public List<MalzemeGrup> MalzemeGrupListe { get; set; }
		public List<MalzemelerVM> MalzemelerVMListe { get; set; }

        public List<KategoriTreeItem> KategoriTree { get; set; }
    }
}
