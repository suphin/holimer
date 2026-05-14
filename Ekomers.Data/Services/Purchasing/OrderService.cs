using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels; 
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims; 

namespace Ekomers.Data.Services
{
	public class OrderService : IOrderService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Sirketler> _sirketRepo;
		private readonly IRepository<Offer> _OfferRepo;
		private readonly IRepository<OfferTur> _OfferTurRepo;
		private readonly IRepository<RequestTur> _requestTurRepo;
		private readonly IRepository<OfferDurum> _OfferDurumRepo;
		private readonly IRepository<RequestUrunler> _requestUrunlerRepo;
		private readonly IRepository<Request> _requestRepo;
		private readonly IRepository<Departman> _departmanRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly IRepository<OfferOdemeTur> _offerOdemeTurRepo;

		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;


		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;

		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public OrderService(ApplicationDbContext context, 
			IHttpContextAccessor httpContextAccessor, 
			IMapper mapper, IHttpClientFactory httpClientFactory, IRepository<Kullanici> userRepo, 
			IRepository<Sirketler> sirketRepo, IRepository<Offer> OfferRepo, IRepository<OfferTur> OfferTurRepo,
			IRepository<RequestTur> requestTurRepo, IRepository<OfferDurum> OfferDurumRepo, 
			IRepository<RequestUrunler> requestUrunlerRepo, IRepository<Request> requestRepo, 
			IRepository<Departman> departmanRepo, IRepository<Musteriler> musterilerRepo, 
			IRepository<Malzeme> urunlerRepo, IRepository<MalzemeGrup> altGrupRepo, 
			IRepository<MalzemeBirim> malzemeBirimRepo, IRepository<MalzemeTipi> malzemeTipiRepo, IRepository<DovizTur> dovizTurRepo, IRepository<OfferOdemeTur> offerOdemeTurRepo)
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
			_departmanRepo = departmanRepo;
			_requestTurRepo = requestTurRepo;
			_dovizTurRepo = dovizTurRepo;
			_offerOdemeTurRepo = offerOdemeTurRepo;
		}
		public IQueryable<OfferVM> GenelListe()
		{
			throw new NotImplementedException();
		}

		public IQueryable<OfferVM> SiparisFirmaGrupListe()
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


						

						 join OfferTur in _OfferTurRepo.GetAll2() on kayit.TurID equals OfferTur.ID
						into OfferTurGroup
						 from OfferTur in OfferTurGroup.DefaultIfEmpty()

						 join dovizTur in _dovizTurRepo.GetAll2() on kayit.DovizTurID equals dovizTur.ID
						 into dovizTurGroup
						 from dovizTur in dovizTurGroup.DefaultIfEmpty()

						 join odemeTur in _offerOdemeTurRepo.GetAll2() on kayit.OdemeTurID equals odemeTur.ID
						 into odemeTurGroup
						 from odemeTur in odemeTurGroup.DefaultIfEmpty()

						 join firma in _musterilerRepo.GetAll2() on kayit.FirmaID equals firma.ID
						 	 into firmaGroup
						 from firma in firmaGroup.DefaultIfEmpty()


						 join requestUrun in _requestUrunlerRepo.GetAll2() on kayit.RequestUrunID equals requestUrun.ID
						 into requestUrunGroup
						 from requestUrun in requestUrunGroup.DefaultIfEmpty()


						 join OfferDurum in _OfferDurumRepo.GetAll2() on requestUrun.OfferDurumID equals OfferDurum.ID
						into OfferDurumGroup
						 from OfferDurum in OfferDurumGroup.DefaultIfEmpty()

						 join request in _requestRepo.GetAll2() on requestUrun.RequestID equals request.ID
						 into requestGroup
						 from request in requestGroup.DefaultIfEmpty()

						 join talepTur in _requestTurRepo.GetAll2() on request.TurID equals talepTur.ID
						 into talepTurGroup
						 from talepTur in talepTurGroup.DefaultIfEmpty()

						
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
							 RedNot = kayit.RedNot,
							 TalepNot = requestUrun.Aciklama,
							 OfferDurumID = requestUrun.OfferDurumID,
							 OfferDurumAd = OfferDurum != null ? OfferDurum.Ad : "",
							 OfferDurumClass = OfferDurum != null ? OfferDurum.Class : "",
							 
							 FirmaID = kayit.FirmaID,
							 FirmaAd = firma != null ? firma.AdSoyad : "",
							 Firma = firma,
							 RequestUrunID = kayit.RequestUrunID,
							 TalepTurID = talepTur != null ? talepTur.ID : 0,
							 TalepTurAd = talepTur != null ? talepTur.Ad : "",
							 RetNot = requestUrun.RedNot,
							 UrunID = requestUrun.UrunID,
							 UrunAd = urun != null ? urun.Ad : "",
							 UrunKod = urun != null ? urun.Kod : "",
							 UrunKdv = urun != null ? (double)urun.Kdv : 0,

							 RequestDate = request != null ? request.RequestDate : new DateTime(1000, 1, 1),
							 requestID = request != null ? request.ID : 0,
							 TTN = requestUrun.TTN,
							 requestUserID = requestUser.Id,
							 requestUserName = requestUser != null ? requestUser.AdSoyad : "",
							 SirketAd = sirket != null ? sirket.SirketAdi : "",
							 Sirket = sirket,
							 SirketID = request.SirketID,

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
							 DovizTurAd = dovizTur != null ? dovizTur.Ad : "",
							 EurRate = kayit.EurRate,
							 UsdRate = kayit.UsdRate,
							 IsLocked = (bool)kayit.IsLocked,
							 OdemeTurID = kayit.OdemeTurID,
							 OdemeTurAd = odemeTur != null ? odemeTur.Ad : "",

							 TeslimTarihi = kayit.TeslimTarihi,
							 OdemeTarihi = kayit.OdemeTarihi,
							 IsSelected = kayit.IsSelected,
							 DosyaID = kayit.DosyaID,

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
			throw new NotImplementedException();
		}

		public Task<OfferVM> VeriGetir(int id)
		{
			throw new NotImplementedException();
		}

		public Task<List<OfferVM>> VeriListele(OfferVM model)
		{
			throw new NotImplementedException();
		}

		public Task<List<OfferVM>> VeriListele()
		{
			throw new NotImplementedException();
		}

		public async Task<PagedResult<SiparisFirmaGrupVM>> VeriListeleFirmaGrup(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				//var query = SiparisFirmaGrupListe(); // IQueryable<OfferVM>



				var data = SiparisFirmaGrupListe()
	.Where(x => x.IsSelected == true)
	.ToList();

				var grouped = data
					.GroupBy(x => new
					{
						x.FirmaID,
						x.FirmaAd
					})
					.Select(g => new SiparisFirmaGrupVM
					{
						FirmaID = g.Key.FirmaID??0,
						FirmaAd = g.Key.FirmaAd??string.Empty,

						TalepSayisi = g.Select(x => x.requestID).Distinct().Count(),
						ToplamUrunSayisi=g.Select(x=>x.RequestUrunID).Distinct().Count(),
						ToplamUrunAdedi = g.Sum(x => x.Miktar),

						ToplamTutar = g.Sum(x => x.Miktar * x.Fiyat),

						Urunler = g.ToList()
					});








				var total = grouped.Count();

				var items =   grouped
					.OrderByDescending(a => a.FirmaID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToList();

				return new PagedResult<SiparisFirmaGrupVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch(Exception ex)
			{
				return new PagedResult<SiparisFirmaGrupVM>
				{
					Items = new List<SiparisFirmaGrupVM>(),
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

		public Task<bool> VeriSil(int id)
		{
			throw new NotImplementedException();
		}

		 

		public Task<PagedResult<OfferVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}
	}
}
