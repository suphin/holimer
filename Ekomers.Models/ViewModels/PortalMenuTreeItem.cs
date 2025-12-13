using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    
	public class PortalMenuTreeItem
	{
		public int ID { get; set; }
		public string Ad { get; set; }
		public string Icon { get; set; }
		public string ControllerName { get; set; }
		public string ActionName { get; set; }
		public int ParentID { get; set; }
		public List<PortalMenuTreeItem> Children { get; set; } = new List<PortalMenuTreeItem>();

		// Alt kategori sayısını hesaplamak için yeni bir property
		public int AltMenuSayisi => Children.Count;
	}
}
