 
 
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
	public class MusterilerService : IMusterilerService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Musteriler> _MusterilerRepo;
		private readonly IRepository<MusteriTip> _musteriTipRepo;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public MusterilerService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Musteriler> MusterilerRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			,IRepository<MusteriTip> musteriTipRepo
			, IRepository<Sehirler> sehirRepo
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_MusterilerRepo = MusterilerRepo;
			_userRepo = userRepo;
			_musteriTipRepo= musteriTipRepo;
			_sehirRepo= sehirRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}

		public async Task<bool> MusterilerAktar(List<Musteriler> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			foreach (var model in liste)
			{
				_MusterilerRepo.Add(model);
			}

			// Değişiklikleri veritabanına yansıt
			_context.SaveChanges();

			return true;
		}

		public IQueryable<MusterilerVM> GenelListe()
		{
			Expression<Func<Musteriler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true ;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _MusterilerRepo.GetAll2(filter)


						 join musteriTip in _musteriTipRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.TipID equals musteriTip.ID
						into musteriTipGroup
						 from musteriTip in musteriTipGroup.DefaultIfEmpty()

						 join sehir in _sehirRepo.GetAll2() on kayit.SehirID equals sehir.ID
						into sehirGroup
						 from sehir in sehirGroup.DefaultIfEmpty()

						 join ilce in _sehirRepo.GetAll2() on kayit.IlceID equals ilce.ID
						 into ilceGroup
						 from ilce in ilceGroup.DefaultIfEmpty()

						 join mahalle in _sehirRepo.GetAll2() on kayit.MahalleID equals mahalle.ID
						into mahalleGroup
						 from mahalle in mahalleGroup.DefaultIfEmpty()

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new MusterilerVM
						 {
							 ID = kayit.ID, 
							 Aciklama=kayit.Aciklama,
							 KayitEden= createUser != null ? createUser.AdSoyad : "",
							 KayitTarihi= kayit.CreateDate != null ? kayit.CreateDate : new DateTime(1000, 1, 1),
							 TipID=kayit.TipID,
							 TipAd=musteriTip.Ad,
							 Adres=kayit.Adres,
							 AdSoyad=kayit.AdSoyad,
							 BizimHesapNo=kayit.BizimHesapNo,
							 Eposta=kayit.Eposta,
							 Telefon=kayit.Telefon,

							 Sehir = sehir != null ? sehir.Ad : "",
							 SehirID =kayit.SehirID == null?0:kayit.SehirID,
							 Ilce=  ilce != null ? ilce.Ad : "",
							 IlceID=kayit.IlceID == null ? 0 : kayit.IlceID,
							 Mahalle = mahalle != null ? mahalle.Ad : "",
							 MahalleID =kayit.MahalleID,

							 PostaKod=kayit.PostaKod,
							 IsKurum=kayit.IsKurum,
							 ParaBirimiID=kayit.ParaBirimiID,
							 LOGICALREF=kayit.LOGICALREF,
							 Ulke=kayit.Ulke,
							 Not=kayit.Not,
							SirketUnvan=kayit.SirketUnvan,
							VergiDairesi=kayit.VergiDairesi,
							VergiNo=kayit.VergiNo,
							KucukResimUrl=kayit.KucukResimUrl,



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



		public Task<MusterilerVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(MusterilerVM model)
		{
			model.Aciklama = model.Aciklama.Replace("\r\n", "");
			Musteriler? existingEntry = _MusterilerRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Musteriler>(model);
				_MusterilerRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_MusterilerRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(MusterilerVM model)
		{
			model.Aciklama = model.Aciklama.Replace("\r\n", "");
			Musteriler? existingEntry = _MusterilerRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Musteriler>(model);
				_MusterilerRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _MusterilerRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<MusterilerVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new MusterilerVM();
			}

			MusterilerVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new MusterilerVM();
			}

			return kayit;
		}

		public async Task<List<MusterilerVM>> VeriListele(MusterilerVM model)
		{
			var liste = GenelListe();



			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama)  
				  
				);
			}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<MusterilerVM>> VeriListele()
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
			Musteriler? kayit = _MusterilerRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _MusterilerRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<MusterilerVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<MusterilerVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<MusterilerVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<MusterilerVM>
				{
					Items = new List<MusterilerVM>(),
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
	}
}
