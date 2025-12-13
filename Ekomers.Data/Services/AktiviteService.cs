 
 
 
using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
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
	public class AktiviteService : IAktiviteService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Aktivite> _AktiviteRepo;
		private readonly IRepository<AktiviteTur> _aktiviteTurRepo;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly ClaimsPrincipal _user;
		private readonly ILoggerService _loggerService;
		private readonly string _userId;
		public AktiviteService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Aktivite> AktiviteRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<AktiviteTur> aktiviteTurRepo
			, IRepository<Sehirler> sehirRepo
			, IRepository<Musteriler> musterilerRepo
			,ILoggerService loggerService
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_AktiviteRepo = AktiviteRepo;
			_userRepo = userRepo;
			_aktiviteTurRepo = aktiviteTurRepo;
			_sehirRepo = sehirRepo;
			_musterilerRepo = musterilerRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
			_loggerService = loggerService;
		}

		public async Task<bool> AktiviteAktar(List<Aktivite> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			foreach (var model in liste)
			{
				_AktiviteRepo.Add(model);
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
		public IQueryable<AktiviteVM> GenelListe()
		{
			Expression<Func<Aktivite, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _AktiviteRepo.GetAll2(filter)


						 join aktiviteTur in _aktiviteTurRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.TurID equals aktiviteTur.ID
						 into aktiviteTurGroup
						 from aktiviteTur in aktiviteTurGroup.DefaultIfEmpty()

						 join musterilerRepo in _musterilerRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.MusteriID equals musterilerRepo.ID
						 into musterilerRepoGroup
						 from musterilerRepo in musterilerRepoGroup.DefaultIfEmpty()



						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new AktiviteVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 TurID = kayit.TurID,
							 TurAd = aktiviteTur != null ? aktiviteTur.Ad : "",
							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
							 MusteriID = kayit.MusteriID,
							 GorevliID = kayit.GorevliID,
							 FirsatID = kayit.FirsatID,
							 TeklifID = kayit.TeklifID,
							 SiparisID = kayit.SiparisID,
							 SorumluID=kayit.SorumluID,
							  Musteri= musterilerRepo ?? new Musteriler(),
							  IsLocked= (bool)kayit.IsLocked,




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



		public Task<AktiviteVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(AktiviteVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Aktivite? existingEntry = _AktiviteRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Aktivite>(model);
				_AktiviteRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_AktiviteRepo.Update(existingEntry);
			}
			 
			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(AktiviteVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Aktivite? existingEntry = _AktiviteRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Aktivite>(model);
				_AktiviteRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _AktiviteRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<AktiviteVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new AktiviteVM();
			}

			AktiviteVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new AktiviteVM();
			}

			return kayit;
		}

		public async Task<List<AktiviteVM>> VeriListele(AktiviteVM model)
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

		public async Task<List<AktiviteVM>> VeriListele()
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
			Aktivite? kayit = _AktiviteRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _AktiviteRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<AktiviteVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<AktiviteVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<AktiviteVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<AktiviteVM>
				{
					Items = new List<AktiviteVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<AktiviteVM>> VeriListeleAsync(AktiviteVM model)
		{
			throw new NotImplementedException();
		}
	}
}
