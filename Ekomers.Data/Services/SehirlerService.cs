using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class SehirlerService : ISehirlerService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IRepository<Dosya> _dosyaRepo;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IMemoryCache _cache;
		private readonly IRepository<Sehirler> _SehirlerRepo;


		public SehirlerService(ApplicationDbContext context,
				 IHttpContextAccessor httpContextAccessor,
				 IMapper mapper,
				 IRepository<Kullanici> userRepo,
				 IRepository<Dosya> dosyaRepo, IRepository<Sehirler> SehirlerRepo
			, IMemoryCache cache)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;
			_dosyaRepo = dosyaRepo;
			_userRepo = userRepo;
			_cache = cache;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_SehirlerRepo = SehirlerRepo;
		}
		private string RoleKey() => _user.IsInRole("Admin") ? "admin" : "user";
		private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();
		private static string AllListCacheKey(string roleKey) => $"Sehirler:All:{roleKey}:v1";

		// Cache süresini burada merkezden yönetebilirsiniz
		private static MemoryCacheEntryOptions CacheOptions() => new()
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
			SlidingExpiration = TimeSpan.FromMinutes(30),
			Size = 1 // (opsiyonel) MemoryCache kompaktlaması için
		};

		// NOT: IQueryable döndürmeyin; ToListAsync ile malzeme edip cache’leyin.
		private async Task<List<SehirlerVM>> _EnsureAllListAsync()
		{
			var roleKey = RoleKey();
			var cacheKey = AllListCacheKey(roleKey);

			// 1) Önce cache’e bak
			if (_cache.TryGetValue(cacheKey, out List<SehirlerVM>? cached) && cached is not null)
				return cached;

			// 2) Aynı key için tek doldurucu olsun
			var gate = _keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
			await gate.WaitAsync();
			try
			{
				// 3) Bu sırada başka biri doldurmuş olabilir, tekrar kontrol
				if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
					return cached;

				// 4) DB'den malzeme et (tek sorgu, tek context üzerinde)
				var list = await GenelListe()
					.AsNoTracking()
					.ToListAsync()
					.ConfigureAwait(false);

				// 5) Cache’e koy
				_cache.Set(cacheKey, list, CacheOptions());

				return list ?? new List<SehirlerVM>(); // CS8603'e karşı güvenli
			}
			finally
			{
				gate.Release();
			}
		}
		private async Task<List<SehirlerVM>> EnsureAllListAsync()
		{
			var roleKey = RoleKey();
			var cacheKey = AllListCacheKey(roleKey);

			// Tek seferde üretmek için GetOrCreateAsync kullanıyoruz
			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.SetOptions(CacheOptions());

				// Sizin mevcut GenelListe() LINQ'unuz + AsNoTracking
				var list = await GenelListe()
					.AsNoTracking()
					.ToListAsync();

				return list;
			});
		}
		public IQueryable<SehirlerVM> GenelListe()
		{
			Expression<Func<Sehirler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from sehir in _SehirlerRepo.GetAll2(filter)

						 join createUser in _userRepo.GetAll2() on sehir.CreateUserID equals createUser.Id
						  into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on sehir.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on sehir.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()




						 select new SehirlerVM
						 {
							 ID = sehir.ID,
							 Ad = sehir.Ad,
							 UstID = sehir.UstID,
							 MinLatitude = sehir.MinLatitude,
							 MinLongitude = sehir.MinLongitude,
							 MaxLatitude = sehir.MaxLatitude,
							 MaxLongitude = sehir.MaxLongitude,
							 MahalleID = sehir.MahalleID,
							 IsActive = (bool)sehir.IsActive,
							 IsDelete = (bool)sehir.IsDelete,

							 CreateUserID = sehir.CreateUserID,
							 CreateDate = sehir.CreateDate != null ? sehir.CreateDate : new DateTime(1000, 1, 1),
							 CreateUserName = createUser != null ? createUser.AdSoyad : "",

							 DeleteUserID = sehir.DeleteUserID,
							 DeleteDate = sehir.DeleteDate,
							 DeleteUserName = deleteUser != null ? deleteUser.AdSoyad : "",

							 UpdateUserID = sehir.UpdateUserID,
							 UpdateDate = sehir.UpdateDate,
							 UpdateUserName = updateUser != null ? updateUser.AdSoyad : "",

						 };

			return result;
		}
		public async Task<List<SehirlerVM>> GetSehirler(int parentId)
		{
			try
			{
				var all = await EnsureAllListAsync();
				return all
					.Where(p => p.UstID == parentId)
					.OrderBy(a => a.ID)
					.ToList();
			}
			catch
			{
				return new List<SehirlerVM>();
			}
		}

		public async Task<List<SehirlerVM>> GetMahalle(int parentId)
		{
			try
			{
				var all = await EnsureAllListAsync();
				return all
					.Where(p => p.UstID == parentId)
					.OrderBy(a => a.ID)
					.ToList();
			}
			catch
			{
				return new List<SehirlerVM>();
			}
		}
		public async Task<List<SehirlerVM>> _GetMahalle(int ParametreID)
		{
			try
			{
				var List = await GenelListe().Where(p => p.UstID == ParametreID).OrderBy(a => a.ID)
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<SehirlerVM>();
			}
		}

		public async Task<List<SehirlerVM>> _GetSehirler(int ParametreID)
		{
			try
			{
				var List = await GenelListe().Where(p => p.UstID == ParametreID).OrderBy(a => a.ID)
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<SehirlerVM>();
			}
		}

		public Task<SehirlerVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(SehirlerVM model)
		{
			Sehirler? kayit = _SehirlerRepo.GetById(model.ID);
			if (kayit == null)
			{

				var newEntry = _mapper.Map<Sehirler>(model);
				//newEntry.CreateDate = DateTime.Now;
				//newEntry.IsActive = true;
				//newEntry.IsDelete = false;
				//newEntry.CreateUserID = _userId;

				_SehirlerRepo.Add(newEntry);
			}
			else
			{
				//model.CreateUserID = _userId;
				//model.IsActive = (bool)existingEntry.IsActive;
				//model.IsDelete = (bool)existingEntry.IsDelete;
				//model.CreateDate = existingEntry.CreateDate == null ? DateTime.Now : existingEntry.CreateDate;

				_mapper.Map(model, kayit);
				_SehirlerRepo.Update(kayit);
			}

			_context.SaveChangesAsync();
			return true;
		}

		public async Task<SehirlerVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new SehirlerVM();
			}

			SehirlerVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new SehirlerVM();
			}

			return kayit;
		}

		public async Task<List<SehirlerVM>> VeriListele(SehirlerVM model)
		{
			var liste = GenelListe();

			if (model.Ad != null)
			{
				liste = liste.Where(p => p.Ad.Contains(model.Ad));
			}
			if (model.UstID != null)
			{
				liste = liste.Where(p => p.UstID == model.UstID);
			}
			if (model.ID != 0)
			{
				liste = liste.Where(p => p.ID == model.ID);
			}
			if (model.MahalleID != null)
			{
				liste = liste.Where(p => p.MahalleID == model.MahalleID);
			}
			var donus = await liste.OrderBy(a => a.ID).Take(1000).ToListAsync();
			return donus;
		}

		public async Task<List<SehirlerVM>> VeriListele()
		{
			try
			{
				var List = await GenelListe().OrderBy(a => a.ID)
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<SehirlerVM>();
			}
		}

		public async Task<bool> VeriSil(SehirlerVM model)
		{
			Sehirler? kayit = _SehirlerRepo.GetById(model.ID);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_SehirlerRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public Task<bool> VeriSil(int id)
		{
			throw new NotImplementedException();
		}

		// İsteyenler için manuel invalidation
		public void InvalidateSehirlerCache()
		{
			_cache.Remove(AllListCacheKey("admin"));
			_cache.Remove(AllListCacheKey("user"));
		}
	}
}
