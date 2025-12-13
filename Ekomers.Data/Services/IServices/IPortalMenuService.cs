using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
   public interface IPortalMenuService:IGenelService<PortalMenuVM,PortalMenu>
    {
		Task<List<PortalMenu>> MenuListele(int MenuID); 
		Task<bool> AltMenuVeriEkle(PortalMenu AltMenu);
		Task<bool> AltMenuSil(int AltMenuID);
		Task<PortalMenu> AltMenuGetir(int AltMenuID);
		Task<List<PortalMenu>> AltMenuListele(int MenuID);
		List<PortalMenuTreeItem> GetMenuTree();
		 

		List<PortalMenuTreeItem> BuildKategoriTree(List<PortalMenu> menuler, int? parentId);
		 

		Task<bool> MenuVeriEkle(PortalMenu Grup);
		Task<bool> MenuSil(int GrupID);
		Task<PortalMenu> MenuGetir(int GrupID);
		Task<List<PortalMenu>> MenuListele();
		Task<List<PortalMenu>> MenuListeleHepsi();
	}
}
