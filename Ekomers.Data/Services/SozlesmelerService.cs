 

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
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class SozlesmelerService : ISozlesmelerService
	{
		private readonly ApplicationDbContext _context;
		private readonly LogoContext _logo;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IMapper _mapper;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IMemoryCache _cache;
		private const string CacheKey = "TumSozlesmelerListesi";
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Sozlesmeler> _sozlesmelerRepo;
		private readonly IRepository<SozlesmelerDurum> _durumRepo;
		private readonly IRepository<SozlesmelerKonu> _konuRepo;
		private readonly IRepository<Sirketler> _sirketlerRepo;
	 

		 
		private readonly IRepository<DovizTur> _dovizTurRepo;
		 
		private readonly IRepository<Departman> _departmanRepo;


		public SozlesmelerService(IMapper mapper, ApplicationDbContext context,
			IHttpContextAccessor httpContextAccessor,  
			IRepository<Kullanici> userRepo,     IRepository<Departman> departmanRepo,
			IRepository<DovizTur> dovizTurRepo, IRepository<Sozlesmeler> sozlesmelerRepo,
			IRepository<SozlesmelerDurum> durumRepo,
			IRepository<SozlesmelerKonu> konuRepo,
			IRepository<Sirketler> sirketlerRepo,
			IWebHostEnvironment hostingEnvironment, IMemoryCache cache, LogoContext logo)
		{
			_httpContextAccessor = httpContextAccessor;
			_user = _httpContextAccessor.HttpContext.User;
			_userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			_cache = cache;
			_context = context;
			_logo = logo;
			_mapper = mapper;
			 _sozlesmelerRepo = sozlesmelerRepo;
			_durumRepo = durumRepo;
			_userRepo = userRepo;
			  
			_departmanRepo = departmanRepo;
			_konuRepo = konuRepo;
			_sirketlerRepo = sirketlerRepo;

			_hostingEnvironment = hostingEnvironment;
			_dovizTurRepo = dovizTurRepo;
		}

		public async Task<IstatistikVM> VeriSayisi()
		{
			var liste = GenelListe();
			var model = new IstatistikVM
			{
				VeriSayisi = liste.Count(),
				AktifSayisi = liste.Where(p => p.IsActive == true).Count(),
				PasifSayisi = liste.Where(p => p.IsActive == false).Count(),
				SilinenSayisi = liste.Where(p => p.IsDelete == true).Count()
			};
			return model;
		}
		public IQueryable<SozlesmelerVM> GenelListe()
		{
			var result = from kayit in _sozlesmelerRepo.GetAll2()

						  

						 join durum in _durumRepo.GetAll2() on kayit.DurumID equals durum.ID
						into durumGroup
						 from durum in durumGroup.DefaultIfEmpty()

						 join konu in _konuRepo.GetAll2() on kayit.KonuID equals konu.ID
						 into konuGroup
						 from konu in konuGroup.DefaultIfEmpty()

						 join sirket in _sirketlerRepo.GetAll2() on kayit.Taraf1ID equals sirket.ID
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


						 select new SozlesmelerVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 AnahtarKelimeler = kayit.AnahtarKelimeler,
							 BaslangicTarih = kayit.BaslangicTarih,
							 Baslik = kayit.Baslik,
							 BitisTarih = kayit.BitisTarih,
							 Durum = durum != null ? durum.Ad : "",
							 DurumID = kayit.DurumID, 
							 Konu = konu != null ? konu.Ad : "",
							 KonuID = kayit.KonuID,
							 SirketID = kayit.SirketID, 
							 Taraf1 =  sirket != null ? sirket.SirketAdi : "",
							 Taraf1ID = kayit.Taraf1ID,
							 Taraf2 = kayit.Taraf2,
							 Taraf2ID = kayit.Taraf2ID,


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

		public Task<SozlesmelerVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(SozlesmelerVM model)
		{

			Sozlesmeler? existingEntry = _sozlesmelerRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Sozlesmeler>(model);
				_sozlesmelerRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_sozlesmelerRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			_cache.Remove(CacheKey);
			return true;
		}
		public async Task<bool> VeriEkleAsync(SozlesmelerVM model)
		{

			Sozlesmeler? existingEntry = _sozlesmelerRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Sozlesmeler>(model);
				_sozlesmelerRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _sozlesmelerRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			_cache.Remove(CacheKey);
			return true;
		}

		public async Task<SozlesmelerVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new SozlesmelerVM();
			}

			SozlesmelerVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new SozlesmelerVM();
			}

			return kayit;
		}
		public async Task<List<SozlesmelerVM>> VeriListele(SozlesmelerVM model)
		{
			var liste = GenelListe();



			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.Konu.Contains(model.Aciklama) ||
				p.Taraf1.Contains(model.Aciklama) ||
				p.Taraf2.Contains(model.Aciklama) ||
				p.Baslik.Contains(model.Aciklama) ||
				p.AnahtarKelimeler.Contains(model.Aciklama) 
				);
			}
			if (model.DurumID != 0)
			{
				liste = liste.Where(p => p.DurumID == model.DurumID);
			}
			if (model.KonuID != 0)
			{
				liste = liste.Where(p => p.KonuID == model.KonuID);
			}
			if (model.Taraf1ID != 0)
			{
				liste = liste.Where(p => p.Taraf1ID == model.Taraf1ID);
			}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}
		public async Task<PagedResult<SozlesmelerVM>> VeriListeleAsync(SozlesmelerVM modelv)
		{
			try
			{

				if (modelv.PageIndex < 1) modelv.PageIndex = 1;
				// mantıklı bir üst sınır koy
				if (modelv.PageSize <= 0 || modelv.PageSize > 1000) modelv.PageSize = 10;

				var query = GenelListe();



				if (modelv.Aciklama != null)
				{
					query = query.Where(p => p.Aciklama.Contains(modelv.Aciklama)  
					);
				}

				var total = await query.CountAsync(modelv.ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((modelv.PageIndex - 1) * modelv.PageSize)
					.Take(modelv.PageSize)
					.ToListAsync(modelv.ct);

				return new PagedResult<SozlesmelerVM>
				{
					Items = items,
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<SozlesmelerVM>
				{
					Items = new List<SozlesmelerVM>(),
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<List<SozlesmelerVM>> VeriListele()
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
			Sozlesmeler? kayit = _sozlesmelerRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _sozlesmelerRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
				_cache.Remove(CacheKey);
			}
			return true;
		}

		public async Task<PagedResult<SozlesmelerVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<SozlesmelerVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderBy(a => a.BitisTarih)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<SozlesmelerVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<SozlesmelerVM>
				{
					Items = new List<SozlesmelerVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}
		public Task<PagedResult<MusterilerVM>> VeriListeleAsync(MusterilerVM model)
		{
			throw new NotImplementedException();
		}

		public async Task<List<SozlesmelerVM>> SozlesmelerAra(string SozlesmelerAd)
		{
			//var results = await  GenelListe().Where(p => p.Ad.Contains(SozlesmelerAd)).ToListAsync();
			//return results;
			var tumListe = await GetAllCachedAsync();
			return tumListe
				.Where(p => p.Aciklama.Contains(SozlesmelerAd, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}


		private async Task<List<SozlesmelerVM>> GetAllCachedAsync()
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
