using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class KategoriTreeItem
    {
        public int ID { get; set; }
        public string Ad { get; set; }
        public string Kod { get; set; }
        public int ParentID { get; set; }
        public List<KategoriTreeItem> Children { get; set; } = new List<KategoriTreeItem>();

        // Alt kategori sayısını hesaplamak için yeni bir property
        public int AltKategoriSayisi => Children.Count;

		public string FullPath { get; set; }
	}
}
