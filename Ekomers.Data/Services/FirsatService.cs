 
 
 
using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models;
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
	public class FirsatService : IFirsatService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Firsat> _FirsatRepo;
		private readonly IRepository<FirsatDurum> _FirsatDurumRepo;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public FirsatService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Firsat> FirsatRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<FirsatDurum> FirsatDurumRepo
			, IRepository<Sehirler> sehirRepo
			, IRepository<Musteriler> musterilerRepo
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_FirsatRepo = FirsatRepo;
			_userRepo = userRepo;
			_FirsatDurumRepo = FirsatDurumRepo;
			_sehirRepo = sehirRepo;
			_musterilerRepo = musterilerRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}

		public async Task<bool> FirsatAktar(List<Firsat> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			foreach (var model in liste)
			{
				_FirsatRepo.Add(model);
			}

			// Değişiklikleri veritabanına yansıt
			_context.SaveChanges();

			return true;
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
		public IQueryable<FirsatVM> GenelListe()
		{
			Expression<Func<Firsat, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _FirsatRepo.GetAll2(filter)


						 join FirsatDurum in _FirsatDurumRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.DurumID equals FirsatDurum.ID
						 into FirsatDurumGroup
						 from FirsatDurum in FirsatDurumGroup.DefaultIfEmpty()

						 join musteriler in _musterilerRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.MusteriID equals musteriler.ID
						 into musterilerGroup
						 from musteriler in musterilerGroup.DefaultIfEmpty()

						 join sorumlu in _userRepo.GetAll2() on kayit.SorumluID equals sorumlu.Id
						 into sorumluGroup
						 from sorumlu in sorumluGroup.DefaultIfEmpty()
						 

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new FirsatVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 DurumID = kayit.DurumID,
							 DurumAd = FirsatDurum != null ? FirsatDurum.Ad : "",
							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
							 MusteriID = kayit.MusteriID,
							 GorevliID = kayit.GorevliID,
							 TeklifID = kayit.TeklifID,
							 SiparisID = kayit.SiparisID,
							 SorumluID= kayit.SorumluID,
							 SorumluAd= sorumlu != null ? sorumlu.AdSoyad : "",
							 Musteri = musteriler,
							 IsLocked = (bool)kayit.IsLocked,




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



		public Task<FirsatVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(FirsatVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Firsat? existingEntry = _FirsatRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Firsat>(model);
				_FirsatRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_FirsatRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(FirsatVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Firsat? existingEntry = _FirsatRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Firsat>(model);
				_FirsatRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _FirsatRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<FirsatVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new FirsatVM();
			}

			FirsatVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new FirsatVM();
			}

			return kayit;
		}

		public async Task<List<FirsatVM>> VeriListele(FirsatVM model)
		{
			var liste = GenelListe();

			if (model.MusteriID != null && model.MusteriID > 0)
			{
				liste = liste.Where(p => p.MusteriID == model.MusteriID);
			}

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama)

				);
			}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<FirsatVM>> VeriListele()
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
			Firsat? kayit = _FirsatRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _FirsatRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<FirsatVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<FirsatVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<FirsatVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<FirsatVM>
				{
					Items = new List<FirsatVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<FirsatVM>> VeriListeleAsync(FirsatVM model)
		{
			throw new NotImplementedException();
		}
	}
}
