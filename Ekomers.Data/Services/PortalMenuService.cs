 
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

 

namespace Ekomers.Data.Services
{
	public class PortalMenuService : IPortalMenuService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;

		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<PortalMenu> _menuRepo;
		private readonly IRepository<PortalMenu> _AltmenuRepo;
		  
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public PortalMenuService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<PortalMenu> menuRepo
			 , IRepository<PortalMenu> AltmenuRepo

			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;
			 
			_menuRepo = menuRepo;
			_AltmenuRepo = AltmenuRepo;
			_userRepo = userRepo;

			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
		 
		}

		public async Task<PortalMenu> AltMenuGetir(int AltMenuID)
		{
			return await _AltmenuRepo.GetByIdAsync(AltMenuID);
		}

		public async Task<List<PortalMenu>> AltMenuListele(int MenuID)
		{
			return await _AltmenuRepo.GetAll2(p => p.ParentID == MenuID && p.IsActive == true && p.IsDelete == false).ToListAsync();
		}

		public async Task<bool> AltMenuSil(int AltMenuID)
		{
			PortalMenu grup = _menuRepo.GetById(AltMenuID);

			if (grup != null)
			{
				grup.DeleteDate = DateTime.Now;
				grup.IsDelete = true;
				grup.DeleteUserID = _userId;
				_menuRepo.Update(grup);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<bool> AltMenuVeriEkle(PortalMenu AltMenu)
		{
			PortalMenu? altgrup = _menuRepo.GetById(AltMenu.ID);
			if (altgrup == null)
			{
				var model = new PortalMenu
				{
					Ad = AltMenu.Ad,
					ControllerName = AltMenu.ControllerName,
					ActionName = AltMenu.ActionName,
					ParentID = AltMenu.ParentID,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId
				};

				_menuRepo.Add(model);
			}
			else
			{
				altgrup.Ad = AltMenu.Ad;
				altgrup.ControllerName = AltMenu.ControllerName;
				altgrup.ActionName = AltMenu.ActionName;
				_menuRepo.Update(altgrup);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public List<PortalMenuTreeItem> BuildKategoriTree(List<PortalMenu> menuler, int? parentId)
		{
			var result = new List<PortalMenuTreeItem>();

			// ParentID'ye göre filtreleme
			var filteredKategoriler = menuler.Where(k => k.ParentID == parentId).ToList();

			foreach (var kategori in filteredKategoriler)
			{
				var item = new PortalMenuTreeItem
				{
					ID = kategori.ID, 
					Ad = kategori.Ad, 
					ParentID = (int)kategori.ParentID,
					Children = BuildKategoriTree(menuler, kategori.ID)  // Recursive çağrı
				};

				result.Add(item);
			}

			return result;
		}
		public List<PortalMenuTreeItem> GetMenuTree()
		{
			var kategoriler = _menuRepo.GetAll2(p => p.IsActive == true && p.IsDelete == false).ToList();
			return BuildKategoriTree(kategoriler, 0);
		}
		 

		public IQueryable<PortalMenuVM> GenelListe()
		{
			Expression<Func<PortalMenu, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _menuRepo.GetAll2(filter)

						  




						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new PortalMenuVM
						 {
							 ID = kayit.ID,
							  Ad=kayit.Ad,
							  ActionName=kayit.ActionName,
							  ControllerName=kayit.ControllerName,
							  ParentID=kayit.ParentID,


							 IsActive = (bool)kayit.IsActive,
							 IsDelete = (bool)kayit.IsDelete,

							 CreateUserID = kayit.CreateUserID,
							 CreateDate = kayit.CreateDate != null ? kayit.CreateDate : new DateTime(1000, 1, 1),
							 CreateUserName = createUser != null ? createUser.AdSoyad : "",

							 DeleteUserID = kayit.DeleteUserID,
							 DeleteDate = kayit.DeleteDate,
							 DeleteUserName = deleteUser != null ? deleteUser.AdSoyad : "",

							 UpdateUserID = kayit.UpdateUserID,
							 UpdateDate = kayit.UpdateDate,
							 UpdateUserName = updateUser != null ? updateUser.AdSoyad : "",

						 };

			return result;
		}

		

		 

		public async Task<PortalMenu> MenuGetir(int GrupID)
		{
			var grup = await _menuRepo.GetByIdAsync(GrupID);
			return grup;
		}

		public async Task<List<PortalMenu>> MenuListele()
		{
			var list = await _menuRepo.GetAll2(p => p.ParentID == 0 && p.IsActive == true && p.IsDelete == false).ToListAsync();
			if (list == null)
			{
				return new List<PortalMenu>();
			}
			return list;
		}

		public async Task<List<PortalMenu>> MenuListeleHepsi()
		{
			var list = await _menuRepo.GetAll2(p => p.IsActive == true && p.IsDelete == false).ToListAsync();
			if (list == null)
			{
				return new List<PortalMenu>();
			}
			return list;
		}

		public async Task<bool> MenuSil(int GrupID)
		{
			PortalMenu grup = _menuRepo.GetById(GrupID);

			if (grup != null)
			{
				grup.DeleteDate = DateTime.Now;
				grup.IsDelete = true;
				grup.DeleteUserID = _userId;
				_menuRepo.Update(grup);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<bool> MenuVeriEkle(PortalMenu Grup)
		{
			PortalMenu? grup = _menuRepo.GetById(Grup.ID);
			if (grup == null)
			{
				var model = new PortalMenu
				{
					Ad = Grup.Ad, 
					ControllerName = Grup.ControllerName, 
					ActionName = Grup.ActionName, 
					ParentID = 0,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId
				};
				_menuRepo.Add(model);
			}
			else
			{
				grup.Ad = Grup.Ad; 
				_menuRepo.Update(grup);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<PortalMenu>> MenuListele(int MenuID)
		{
			var list = await _menuRepo.GetAll2(p => p.IsActive == true && p.IsDelete == false && p.ParentID == MenuID).ToListAsync();
			if (list == null)
			{
				return new List<PortalMenu>();
			}
			return list;
		}

		 

		public Task<PortalMenuVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(PortalMenuVM model)
		{
			PortalMenu? existingEntry = _menuRepo.GetById(model.ID);
			if (existingEntry == null)
			{

				var newEntry = _mapper.Map<PortalMenu>(model);


				_menuRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_menuRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<PortalMenuVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new PortalMenuVM();
			}

			PortalMenuVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new PortalMenuVM();
			}

			return kayit;
		}

		public async Task<List<PortalMenuVM>> VeriListele(PortalMenuVM model)
		{
			var liste = GenelListe();

			//if (model.KurumTuruID != 0)
			//{
			//	liste = liste.Where(p => p.KurumTuruID == model.KurumTuruID);
			//}
			//if (model.DenetimTuruID != 0)
			//{
			//	liste = liste.Where(p => p.DenetimTuruID == model.DenetimTuruID);
			//}

			//if (model.Aciklama != null)
			//{
			//	liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
			//	p.DenetimKonusu.Contains(model.Aciklama) ||
			//	p.DenetimTuru.Contains(model.Aciklama) ||
			//	p.KurumTuru.Contains(model.Aciklama) ||
			//	p.Adres.Contains(model.Aciklama));
			//}
			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<PortalMenuVM>> VeriListele()
		{
			try
			{
				var List = await GenelListe().OrderByDescending(a => a.ID)
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<PortalMenuVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			PortalMenu? kayit = _menuRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_menuRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

	 


		#region "Raporlar"




		#endregion

	}
}