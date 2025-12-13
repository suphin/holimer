using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
   public class PortalMenu:BaseEntity
    {
		public string Ad { get; set; }
		public string? ControllerName { get; set; }
		public string? ActionName { get; set; }
		public int ParentID { get; set; }
		
	}

	public class PortalMenuVM : BaseVM		
	{
		public string Ad { get; set; }
		public string? ControllerName { get; set; }
		public string? ActionName { get; set; }
		public int ParentID { get; set; }
		public PortalMenu PortalMenu { get; set; }
		public List<PortalMenu> PortalMenuListe { get; set; }
		public List<PortalMenuVM> PortalMenuVMListe { get; set; }
		public List<PortalMenuTreeItem> PortalMenuTree { get; set; }
	}
}
