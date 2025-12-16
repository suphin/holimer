 
using AutoMapper;
using Azure;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.FilterVM;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class PersonelService : IPersonelService
	{
		private readonly ApplicationDbContext _context;
		private readonly LogoContext _logo;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IMapper _mapper;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IMemoryCache _cache;
		private const string CacheKey = "TumPersonelListesi";
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Personel> _PersonelRepo;

		private readonly IRepository<Departman> _departmanRepo;
		private readonly IRepository<Sirketler> _sirketRepo;


		public PersonelService(IMapper mapper, ApplicationDbContext context,
			IHttpContextAccessor httpContextAccessor, 
			 
			IRepository<Kullanici> userRepo, 
			IRepository<Personel> PersonelRepo,
			IRepository<Departman> departmanRepo,
			IRepository<Sirketler> sirketRepo,
			IWebHostEnvironment hostingEnvironment, IMemoryCache cache, LogoContext logo)
		{
			_httpContextAccessor = httpContextAccessor;
			_user = _httpContextAccessor.HttpContext.User;
			_userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			_cache = cache;
			_context = context;
			_logo = logo;
			_mapper = mapper;
			_userRepo = userRepo;
			_PersonelRepo = PersonelRepo;
			_departmanRepo = departmanRepo;
			_sirketRepo = sirketRepo;
			_hostingEnvironment = hostingEnvironment;
			
		}

		
		public IQueryable<PersonelVM> GenelListe()
		{
			var result = from kayit in _PersonelRepo.GetAll2()

						 join user in _userRepo.GetAll2() on kayit.UserID equals user.Id
						into userGroup
						 from user in userGroup.DefaultIfEmpty()

						 join departman in _departmanRepo.GetAll2() on user.DepartmanID equals departman.ID
					   into departmanGroup
						 from departman in departmanGroup.DefaultIfEmpty()

						 join sirket in _sirketRepo.GetAll2() on user.SirketID equals sirket.ID
						 into sirketGroup
						 from sirket in sirketGroup.DefaultIfEmpty()


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()


						 select new PersonelVM
						 {
							 ID = kayit.ID,
							 UserID = kayit.UserID,
							 AdSoyad = kayit.AdSoyad,
							 Email = kayit.Email,
							 Telefon = kayit.Telefon,
							 KullaniciAdi = kayit.KullaniciAdi,
							 Sifre = kayit.Sifre,
							 Not = kayit.Not,
							 DepartmanAd = departman != null ? departman.Ad : "",
							 SirketAd = sirket != null ? sirket.SirketAdi : "",
							 




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

		public Task<PersonelVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(PersonelVM model)
		{

			Personel? existingEntry = _PersonelRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Personel>(model);
				_PersonelRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_PersonelRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			_cache.Remove(CacheKey);
			return true;
		}
		public async Task<bool> VeriEkleAsync(PersonelVM model)
		{

			Personel? existingEntry = _PersonelRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Personel>(model);
				_PersonelRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _PersonelRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			_cache.Remove(CacheKey);
			return true;
		}

		public async Task<PersonelVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new PersonelVM();
			}

			PersonelVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new PersonelVM();
			}

			return kayit;
		}
		public async Task<List<PersonelVM>> VeriListele(PersonelVM model)
		{

			

			var liste = GenelListe();


			//if (model.Aciklama != null)
			if (!string.IsNullOrWhiteSpace(model.Not))
			{
				liste = liste.Where(p => p.Not.Contains(model.Not) ||
				p.AdSoyad.Contains(model.Not) ||
				p.Email.Contains(model.Not)
				);
			}
			//if (model.GrupID != 0)
			//{
			//	liste = liste.Where(p =>p.AltGrupID==model.GrupID);
			//}


			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}
		public async Task<PagedResult<PersonelVM>> VeriListeleAsync(PersonelVM modelv)
		{
			try
			{

				if (modelv.PageIndex < 1) modelv.PageIndex = 1;
				// mantıklı bir üst sınır koy
				if (modelv.PageSize <= 0 || modelv.PageSize > 1000) modelv.PageSize = 10;

				var query = GenelListe();



				if (modelv.Not != null)
				{
					query = query.Where(p => p.Not.Contains(modelv.Not) ||
					p.AdSoyad.Contains(modelv.Not)
					);
				}

				var total = await query.CountAsync(modelv.ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((modelv.PageIndex - 1) * modelv.PageSize)
					.Take(modelv.PageSize)
					.ToListAsync(modelv.ct);

				return new PagedResult<PersonelVM>
				{
					Items = items,
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<PersonelVM>
				{
					Items = new List<PersonelVM>(),
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<List<PersonelVM>> VeriListele()
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

				return [];
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			Personel? kayit = _PersonelRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _PersonelRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
				_cache.Remove(CacheKey);
			}
			return true;
		}

		public async Task<PagedResult<PersonelVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<PersonelVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderBy(a => a.AdSoyad)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<PersonelVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<PersonelVM>
				{
					Items = new List<PersonelVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}
		

		public async Task<List<PersonelVM>> PersonelAra(string PersonelAd)
		{
			//var results = await  GenelListe().Where(p => p.Ad.Contains(PersonelAd)).ToListAsync();
			//return results;
			var tumListe = await GetAllCachedAsync();
			return tumListe
				.Where(p => p.AdSoyad.Contains(PersonelAd, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}
		public async Task<List<PersonelVM>> Personeller()
		{
			//var results = await  GenelListe().Where(p => p.Ad.Contains(PersonelAd)).ToListAsync();
			//return results;
			var tumListe = await GetAllCachedAsync();
			return tumListe
				.Where(p => p.IsActive == true && p.IsDelete == false)
				.ToList();
		}

		private async Task<List<PersonelVM>> GetAllCachedAsync()
		{
			return await _cache.GetOrCreateAsync(CacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
				// veritabanından bir kez çek
				var list = await GenelListe().ToListAsync();
				return list;
			});
		}


	}
}
