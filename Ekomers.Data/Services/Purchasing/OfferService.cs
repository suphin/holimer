using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Ekomers.Data.Services
{
	public class OfferService : IOfferService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Sirketler> _sirketRepo;
		private readonly IRepository<Offer> _OfferRepo;
		private readonly IRepository<OfferTur> _OfferTurRepo;
		private readonly IRepository<OfferDurum> _OfferDurumRepo;
	private readonly IRepository<RequestUrunler> _requestUrunlerRepo;
		private readonly IRepository<Request> _requestRepo;
		private readonly IRepository<Departman> _departmanRepo;

		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
 

		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		 
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public OfferService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo
			, IRepository<Offer> OfferRepo
			, IRepository<Departman> departmanRepo
			, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<OfferDurum> OfferDurumRepo
			, IRepository<OfferTur> OfferTurRepo
			, IRepository<Sirketler> sirketRepo 
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			 ,IRepository<Musteriler> musterilerRepo
			, IRepository<RequestUrunler> requestUrunlerRepo
			, IRepository<Request> requestRepo
		 
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_OfferRepo = OfferRepo;
			_OfferTurRepo = OfferTurRepo;
			_userRepo = userRepo;
			_OfferDurumRepo = OfferDurumRepo;
			_sirketRepo = sirketRepo;
			_musterilerRepo = musterilerRepo;
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			 _requestUrunlerRepo = requestUrunlerRepo;
			_requestRepo = requestRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}

		 

	 

		public IQueryable<OfferVM> GenelListe()
		{
			Expression<Func<Offer, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _OfferRepo.GetAll2(filter)


						 join OfferDurum in _OfferDurumRepo.GetAll2() on kayit.DurumID equals OfferDurum.ID
						 into OfferDurumGroup
						 from OfferDurum in OfferDurumGroup.DefaultIfEmpty()

						 join OfferTur in _OfferTurRepo.GetAll2() on kayit.TurID equals OfferTur.ID
						into OfferTurGroup
						 from OfferTur in OfferTurGroup.DefaultIfEmpty()

						 join firma in _musterilerRepo.GetAll2() on kayit.FirmaID equals firma.ID
						 	 into firmaGroup
						 from firma in firmaGroup.DefaultIfEmpty()


						 join requestUrun in _requestUrunlerRepo.GetAll2() on kayit.RequestUrunID equals requestUrun.ID
						 into requestUrunGroup
						 from requestUrun in requestUrunGroup.DefaultIfEmpty()

						 join request in _requestRepo.GetAll2() on requestUrun.RequestID equals request.ID
						 into requestGroup
						 from request in requestGroup.DefaultIfEmpty()


						 join urun in _urunlerRepo.GetAll2() on requestUrun.UrunID equals urun.ID
							into urunGroup
						 from urun in urunGroup.DefaultIfEmpty()


						 join sirket in _sirketRepo.GetAll2() on request.SirketID equals sirket.ID
						 into sirketGroup
						 from sirket in sirketGroup.DefaultIfEmpty()


						 join requestUser in _userRepo.GetAll2() on request.CreateUserID equals requestUser.Id
						into requestUserGroup
						 from requestUser in requestUserGroup.DefaultIfEmpty()

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new OfferVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 TalepNot=requestUrun.Aciklama,
							 DurumID = kayit.DurumID,
							 DurumAd = OfferDurum != null ? OfferDurum.Ad : "",
							 DurumClass = OfferDurum != null ? OfferDurum.Class : "",
							 FirmaID = kayit.FirmaID,
							 FirmaAd = firma != null ? firma.AdSoyad : "",
							 Firma = firma,
							 RequestUrunID = kayit.RequestUrunID,
							  
							 UrunID = requestUrun.UrunID,
							 UrunAd = urun != null ? urun.Ad : "",
							 UrunKod = urun != null ? urun.Kod : "",
							 UrunKdv= urun != null ? (double)urun.Kdv : 0,

							 RequestDate = request != null ? request.RequestDate : new DateTime(1000, 1, 1),
							 requestID = request != null ? request.ID : 0,
							 TTN=requestUrun.TTN,
							 requestUserID = requestUser.Id,
							 requestUserName = requestUser != null ? requestUser.AdSoyad : "",
							 SirketAd = sirket != null ? sirket.SirketAdi : "",
							 Sirket = sirket,
							 SirketID=request.SirketID,
							 
							 SatinalmaMuduru = sirket.SatinalmaMuduru,
							 MuhasebeMuduru = sirket.MuhasebeMuduru,
							 GenelMudur = sirket.GenelMudur,
							 GenelKoordinator = sirket.GenelKoordinator,
							 SirketAdres = sirket.SirketAdres,
							 SirketVergiDairesi = sirket.SirketVergiDairesi,
							 SirketVergiNo = sirket.SirketVergiNo,
							 SirketWebSitesi = sirket.SirketWebSitesi,

							 TurID = kayit.TurID,
							 TurAd = OfferTur != null ? OfferTur.Ad : "",
							 Vade = kayit.Vade,
							 Miktar = kayit.Miktar,
							 Fiyat = kayit.Fiyat,
							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),

							 DovizTurID = kayit.DovizTurID,
							 EurRate = kayit.EurRate,
							 UsdRate = kayit.UsdRate,
							 IsLocked = (bool)kayit.IsLocked,
							 OdemeTurID = kayit.OdemeTurID,
							 TeslimTarihi = kayit.TeslimTarihi,
							 OdemeTarihi=kayit.OdemeTarihi,
							 IsSelected = kayit.IsSelected,
						

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



		public Task<OfferVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(OfferVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Offer? existingEntry = _OfferRepo.GetById(model.ID);
			var newEntry = new Offer();
			if (existingEntry == null)
			{
				 newEntry = _mapper.Map<Offer>(model);
				newEntry.DosyaID = "PR-" + DateTime.Now.Ticks;
				_OfferRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_OfferRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			var ID = newEntry.ID;
			return true;
		}
		public async Task<bool> VeriEkleAsync(OfferVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Offer? existingEntry = _OfferRepo.GetById(model.ID);
			var newEntry = new Offer();
			if (existingEntry == null)
			{
				  newEntry = _mapper.Map<Offer>(model);
				newEntry.DosyaID = "PR-" + DateTime.Now.Ticks;
				_OfferRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _OfferRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			var ID = newEntry.ID;
			return true;
		}
		public async Task<int> VeriEkleReturnIDAsync(OfferVM model)
		{
			model.Not = (model.Not ?? string.Empty).Replace("\r\n", "");

			Offer entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _OfferRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Offer {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _OfferRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Offer>(model);
				entity.DosyaID = "PR-" + DateTime.Now.Ticks;
				// Tercihen async ekleme
				await _OfferRepo.AddAsync(entity);
				// Eğer Add async değilse: _OfferRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}

		public async Task<OfferVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new OfferVM();
			}

			OfferVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new OfferVM();
			}

			return kayit;
		}

		public async Task<List<OfferVM>> VeriListele(OfferVM model)
		{
			var liste = GenelListe();
			if (model.RequestUrunID != 0)
			{
				liste = liste.Where(p => p.RequestUrunID==model.RequestUrunID);
			}
			if (model.UrunID != 0)
			{
				liste = liste.Where(p => p.UrunID == model.UrunID);
			}
			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama)

				);
			}

			if (model.PageSize != 0)
			{
				return   liste.OrderByDescending(a => a.ID).Take(model.PageSize).ToList();
			}
			else
			{
				return  liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			}
			 
		 
		}

		public async Task<List<OfferVM>> VeriListele()
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
			Offer? kayit = _OfferRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _OfferRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<OfferVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<OfferVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<OfferVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<OfferVM>
				{
					Items = new List<OfferVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}
		 
		public Task<PagedResult<OfferVM>> VeriListeleAsync(OfferVM model)
		{
			throw new NotImplementedException();
		}
		 
	}
}
