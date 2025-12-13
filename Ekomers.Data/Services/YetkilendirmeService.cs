 

 
using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class YetkilendirmeService : IYetkilendirmeService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Yetkilendirme> _AuthorizationRepo;
		private readonly IRepository<AuthorizationCategory> _kategoriRepo;

		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public YetkilendirmeService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Yetkilendirme> AuthorizationRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			,IRepository<AuthorizationCategory> kategoriRepo

			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_AuthorizationRepo = AuthorizationRepo;
			_userRepo = userRepo;
			_kategoriRepo= kategoriRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}
 

		public IQueryable<YetkilendirmeVM> GenelListe()
		{
			Expression<Func<Yetkilendirme, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _AuthorizationRepo.GetAll2(filter)


						 join kategori in _kategoriRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.KategoriID equals kategori.ID
						into kategoriGroup
						 from kategori in kategoriGroup.DefaultIfEmpty()


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new YetkilendirmeVM
						 {
							 ID = kayit.ID,
							 Ad=kayit.Ad,
							 Aciklama=kayit.Aciklama,
							 KategoriID=kayit.KategoriID,	
							 KategoriAd=kategori.Ad,
							 ClaimType=kayit.ClaimType,
							 ClaimName=kayit.ClaimName,
							 PolicyName=kayit.PolicyName,





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



		public Task<YetkilendirmeVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(YetkilendirmeVM model)
		{
			if (model.Aciklama != null)
			{
				model.Aciklama = model.Aciklama.Replace("\r\n", "");
			}

			Yetkilendirme? existingEntry = _AuthorizationRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Yetkilendirme>(model);
				_AuthorizationRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_AuthorizationRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<YetkilendirmeVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new YetkilendirmeVM();
			}

			YetkilendirmeVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new YetkilendirmeVM();
			}

			return kayit;
		}

		public async Task<List<YetkilendirmeVM>> VeriListele(YetkilendirmeVM model)
		{
			var liste = GenelListe();

			if (model.KategoriID != 0)
			{
				liste = liste.Where(p => p.KategoriID == model.KategoriID);
			}

		 

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.Ad.Contains(model.Aciklama) ||
				p.ClaimType.Contains(model.Aciklama) ||
				p.ClaimName.Contains(model.Aciklama) ||
				p.PolicyName.Contains(model.Aciklama) ||
				p.KategoriAd.Contains(model.Aciklama) 
				);
			}

			var donus = await liste.OrderByDescending(a => a.ID).Take(1000).ToListAsync();
			return donus;
		}

		public async Task<List<YetkilendirmeVM>> VeriListele()
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

				return new List<YetkilendirmeVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			Yetkilendirme? kayit = _AuthorizationRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_AuthorizationRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}
	}
}

