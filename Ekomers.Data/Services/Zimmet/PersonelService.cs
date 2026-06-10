 
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
		private readonly BordroContext _bordro;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IMapper _mapper;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IMemoryCache _cache;
		private const string CacheKey = "TumPersonelListesi";
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Personel> _PersonelRepo;
		private readonly IRepository<PersonelDurum> _PersonelDurumRepo;
		private readonly IRepository<PersonelGorev> _PersonelGorevRepo;

		private readonly IRepository<Departman> _departmanRepo;
		private readonly IRepository<Sirketler> _sirketRepo;


		public PersonelService(IMapper mapper, ApplicationDbContext context,
			IHttpContextAccessor httpContextAccessor, 
			 
			IRepository<Kullanici> userRepo, 
			IRepository<Personel> PersonelRepo,
			IRepository<Departman> departmanRepo,
			IRepository<Sirketler> sirketRepo,
			IWebHostEnvironment hostingEnvironment, IMemoryCache cache
			, IRepository<PersonelDurum> PersonelDurumRepo, IRepository<PersonelGorev> PersonelGorevRepo,BordroContext bordro
			)
		{
			_httpContextAccessor = httpContextAccessor;
			_user = _httpContextAccessor.HttpContext.User;
			_userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			_cache = cache;
			_context = context;
			_bordro = bordro;
			_mapper = mapper;
			_userRepo = userRepo;
			_PersonelRepo = PersonelRepo;
			_PersonelDurumRepo = PersonelDurumRepo;
			_PersonelGorevRepo = PersonelGorevRepo;
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

						 join departman in _departmanRepo.GetAll2() on kayit.DepartmanID equals departman.ID
					   into departmanGroup
						 from departman in departmanGroup.DefaultIfEmpty()

						 join sirket in _sirketRepo.GetAll2() on kayit.SirketID equals sirket.ID
						 into sirketGroup
						 from sirket in sirketGroup.DefaultIfEmpty()

						 join durum in _PersonelDurumRepo.GetAll2() on kayit.DurumID equals durum.ID
						 into durumGroup
						 from durum in durumGroup.DefaultIfEmpty()

						 join gorev in _PersonelGorevRepo.GetAll2() on kayit.GorevID equals gorev.ID
						 into gorevGroup
						 from gorev in gorevGroup.DefaultIfEmpty()


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
							 Not = kayit.Not,
							 DepartmanAd = departman != null ? departman.Ad : "",
							 SirketAd = sirket != null ? sirket.SirketKisaAdi : "",
							 DogumTarihi = kayit.DogumTarihi,
							 IseBaslamaTarihi=kayit.IseBaslamaTarihi,
							 AyrilisTarihi=kayit.AyrilisTarihi,
							 Adres = kayit.Adres,
							 Tckn = kayit.Tckn,
							 PersonelKod=kayit.PersonelKod,
							 TelefonSirket=kayit.TelefonSirket,
							 DepartmanID=kayit.DepartmanID,
							 SirketID=kayit.SirketID,
							 PersonelDurum = durum != null ? new PersonelDurum { ID = durum.ID, Ad = durum.Ad } : null,
							 PersonelGorev = gorev != null ? new PersonelGorev { ID = gorev.ID, Ad = gorev.Ad } : null,
							 DurumID=kayit.DurumID,
							 GorevID = kayit.GorevID,
							 Gorev = gorev != null ? gorev.Ad : "",
							 Fotograf=kayit.Fotograf,
							 Cinsiyet=kayit.Cinsiyet,

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

		public async Task<PagedResult<PersonelVM>> VeriListeleAsync(PersonelVM model, int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe().Where(p => p.DurumID == model.DurumID); // IQueryable<PersonelVM>

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

		public async Task<bool> BordroPersonelAktar()
		{
			try
			{
				var bordroPersoneller = await _bordro.Personel.ToListAsync();

				var mevcutPersoneller = await _PersonelRepo
					.GetAll2()
					.ToListAsync();

				// Mevcut personel kodlarını HashSet'e al (hızlı arama için)
				var mevcutKodlar = mevcutPersoneller
					.Where(x => !string.IsNullOrEmpty(x.PersonelKod))
					.Select(x => x.PersonelKod!)
					.ToHashSet();

				var yeniPersoneller = bordroPersoneller
					.Where(x => !string.IsNullOrEmpty(x.CODE)
							 && !mevcutKodlar.Contains(x.CODE))
					.Select(x => new Personel
					{
						PersonelKod = x.CODE,
						AdSoyad = $"{x.NAME} {x.SURNAME}".Trim(),

						DogumTarihi = x.BIRTHDATE ?? DateTime.MinValue,
						IseBaslamaTarihi = x.INDATE ?? DateTime.MinValue,
						AyrilisTarihi = x.OUTDATE ?? DateTime.MinValue,
						IsActive=true,
						IsDelete=false,
						Cinsiyet = x.SEX ?? 0,

						// Varsayılan değerler
						DepartmanID = 0,
						SirketID = x.FIRMNR ?? 0,
						BolumID = 0,
						GorevID = 0,
						
						DurumID = x.TYP ?? 0,
					})
					.ToList();

				if (yeniPersoneller.Any())
				{
					await _context.Personel.AddRangeAsync(yeniPersoneller);
					await _context.SaveChangesAsync();
				}

				return true;
			}
			catch (Exception ex)
			{

				return false;
			}
			
		}

		public Task<PagedResult<PersonelVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}

		public void FotoYukle(PersonelVM model)
		{
			var personel = _PersonelRepo.GetById(model.ID);
			if (personel != null)
			{
				personel.Fotograf = model.Fotograf;
				_PersonelRepo.Update(personel);
				_context.SaveChanges();
			}
		}
	}
}
