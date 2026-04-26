using AutoMapper;
using Azure;
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
	public class RequestService : IRequestService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Sirketler> _sirketRepo;
		private readonly IRepository<Request> _RequestRepo;
		private readonly IRepository<RequestTur> _RequestTurRepo;
		private readonly IRepository<RequestDurum> _RequestDurumRepo;
		private readonly IRepository<OfferDurum> _OfferDurumRepo;
		private readonly IRepository<Offer> _OfferRepo;
		 


		private readonly IRepository<RequestUrunler> _RequestUrunlerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
 

		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		 
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public RequestService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Request> RequestRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<RequestDurum> RequestDurumRepo
			, IRepository<RequestTur> RequestTurRepo
		 , IRepository<Sirketler> sirketRepo
			, IRepository<RequestUrunler> RequestUrunlerRepo
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			 , IRepository<OfferDurum> OfferDurumRepo
			 , IRepository<Offer> OfferRepo

			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_RequestRepo = RequestRepo;
			_RequestTurRepo = RequestTurRepo;
			_userRepo = userRepo;
			_RequestDurumRepo = RequestDurumRepo;
			_sirketRepo = sirketRepo;
			_OfferDurumRepo = OfferDurumRepo;
			   _RequestUrunlerRepo = RequestUrunlerRepo;
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			_OfferRepo = OfferRepo;

			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}

		 

	 

		public IQueryable<RequestVM> GenelListe()
		{
			Expression<Func<Request, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _RequestRepo.GetAll2(filter)


						 join RequestDurum in _RequestDurumRepo.GetAll2() on kayit.DurumID equals RequestDurum.ID
						 into RequestDurumGroup
						 from RequestDurum in RequestDurumGroup.DefaultIfEmpty()

						 join RequestTur in _RequestTurRepo.GetAll2() on kayit.TurID equals RequestTur.ID
						into RequestTurGroup
						 from RequestTur in RequestTurGroup.DefaultIfEmpty()

						 join sirket in _sirketRepo.GetAll2() on kayit.SirketID equals sirket.ID
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

						 select new RequestVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 DurumID = kayit.DurumID,
							 DurumAd = RequestDurum != null ? RequestDurum.Ad : "",
							 DurumClass = RequestDurum != null ? RequestDurum.Class : "",
							 SirketAd = sirket != null ? sirket.SirketAdi : "",
							 SirketID = kayit.SirketID,
							 SirketAdres = sirket.SirketAdres,
							 SirketVergiDairesi = sirket.SirketVergiDairesi,
							 SirketVergiNo = sirket.SirketVergiNo,
							 SirketWebSitesi = sirket.SirketWebSitesi,

							 TurID = kayit.TurID,
							 TurAd = RequestTur != null ? RequestTur.Ad : "",

							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
							 RequestDate=kayit.RequestDate,
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



		public Task<RequestVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(RequestVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Request? existingEntry = _RequestRepo.GetById(model.ID);
			var newEntry = new Request();
			if (existingEntry == null)
			{
				 newEntry = _mapper.Map<Request>(model);
				newEntry.DosyaID = "PR-" + DateTime.Now.Ticks;
				_RequestRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_RequestRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			var ID = newEntry.ID;
			return true;
		}
		public async Task<bool> VeriEkleAsync(RequestVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Request? existingEntry = _RequestRepo.GetById(model.ID);
			var newEntry = new Request();
			if (existingEntry == null)
			{
				  newEntry = _mapper.Map<Request>(model);
				newEntry.DosyaID = "PR-" + DateTime.Now.Ticks;
				_RequestRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _RequestRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			var ID = newEntry.ID;
			return true;
		}
		public async Task<int> VeriEkleReturnIDAsync(RequestVM model)
		{
			model.Not = (model.Not ?? string.Empty).Replace("\r\n", "");

			Request entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _RequestRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Request {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _RequestRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Request>(model);
				entity.DosyaID = "PR-" + DateTime.Now.Ticks;
				// Tercihen async ekleme
				await _RequestRepo.AddAsync(entity);
				// Eğer Add async değilse: _RequestRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}

		public async Task<RequestVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new RequestVM();
			}

			RequestVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new RequestVM();
			}

			return kayit;
		}

		public async Task<List<RequestVM>> VeriListele(RequestVM model)
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

		public async Task<List<RequestVM>> VeriListele()
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
			Request? kayit = _RequestRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _RequestRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<RequestVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<RequestVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<RequestVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<RequestVM>
				{
					Items = new List<RequestVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}
		public async Task<PagedResult<RequestVM>> VeriListeleOnayAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe().Where(p=>p.DurumID==(int)EnumRequestDurum.OnayBekliyor); // IQueryable<RequestVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<RequestVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<RequestVM>
				{
					Items = new List<RequestVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<RequestVM>> VeriListeleAsync(RequestVM model)
		{
			throw new NotImplementedException();
		}


		public async Task<bool> RequestUrunEkle(RequestUrunlerVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			 
				var model = new RequestUrunler
				{
					UrunID = modelv.UrunID,
					RequestID = modelv.RequestID,
				 
					Miktar = modelv.Miktar,
					Aciklama=modelv.Aciklama,
					 
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId,
					DosyaID = newGuid
				};
				_RequestUrunlerRepo.Add(model);
			 

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> RequestUrunGuncelle(RequestUrunlerVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			var existingEntry= _RequestUrunlerRepo.GetById(modelv.ID);
			_mapper.Map(modelv, existingEntry);

			_RequestUrunlerRepo.Update(existingEntry);


			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<List<RequestUrunlerVM>> RequestUrunlerGetir(int RequestID)
		{
			if (RequestID!=0)
			{
				try
				{
					var list = await RequestUrunlerGenelListe().Where(p => p.RequestID == RequestID).ToListAsync();	
					return list;
				}
				catch (Exception ex)
				{

					return new List<RequestUrunlerVM>();
				}
				 

				
			}
			else
			{
				return new List<RequestUrunlerVM>();
			}
		}


		public IQueryable<RequestUrunlerVM> RequestUrunlerGenelListe()
		{
			Expression<Func<RequestUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _RequestUrunlerRepo.GetAll2(filter)

						 join request in _RequestRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.RequestID equals request.ID
						 into requestGroup
						 from request in requestGroup.DefaultIfEmpty()

						 join sirket in _sirketRepo.GetAll2() on request.SirketID equals sirket.ID into sirketGroup
						 						 from sirket in sirketGroup.DefaultIfEmpty()

						join durum in _RequestDurumRepo.GetAll2() on request.DurumID equals durum.ID into durumGroup
						from durum in durumGroup.DefaultIfEmpty()

						 join OfferDurum in _OfferDurumRepo.GetAll2() on kayit.OfferDurumID equals OfferDurum.ID
						  into OfferDurumGroup
						 from OfferDurum in OfferDurumGroup.DefaultIfEmpty()
 
						 


						 join tur in _RequestTurRepo.GetAll2() on request.TurID equals tur.ID into turGroup
						from tur in turGroup.DefaultIfEmpty()

						 join urunler in _urunlerRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.UrunID equals urunler.ID
						 into urunlerGroup
						 from urunler in urunlerGroup.DefaultIfEmpty()

						
						 join birim in _malzemeBirimRepo.GetAll2() on urunler.BirimID equals birim.ID
						 into birimGroup
						 from birim in birimGroup.DefaultIfEmpty()

						 

						 join tip in _malzemeTipiRepo.GetAll2() on urunler.TipID equals tip.ID
						into tipGroup
						 from tip in tipGroup.DefaultIfEmpty()

						 

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new RequestUrunlerVM
						 {
							 ID = kayit.ID,
							 UrunAd = urunler.Ad != null ? urunler.Ad : "",
							 UrunKod = urunler.Kod != null ? urunler.Kod : "",  
							 
							 BirimID = urunler.BirimID,
							 BirimAd = birim.Ad,
							 TipAd = tip.Ad,
							 TipID = tip.ID, 
							  
							 Aciklama=kayit.Aciklama,
							 RequestID = kayit.RequestID,
							 
							 UrunID = kayit.UrunID,
							 Miktar = (double)kayit.Miktar,
							 MiktarSon= (double)kayit.MiktarSon,
							 OnayliMi=kayit.OnayliMi,
							 RequestDate = request != null ? request.RequestDate : new DateTime(1000, 1, 1),
							 RequestDurumAd = durum != null ? durum.Ad : "",
							 RequestDurumID=(int)request.DurumID,
							 	 RequestDurumClass = durum != null ? durum.Class : "",
								 RequestTurID = (int)request.TurID,
								 RequestTurAd = tur != null ? tur.Ad : "",

								 OfferDurumID=kayit.OfferDurumID,
								 OfferDurumAd=OfferDurum.Ad,
								 DurumClass=OfferDurum.Class, 

							 SirketID =(int)request.SirketID,
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

		public IQueryable<RequestUrunlerVM> OfferGenelListe()
		{
			Expression<Func<RequestUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false ;
			}
			var result = from kayit in _RequestUrunlerRepo.GetAll2(filter) where  kayit.OfferDurumID!=0 

						 join request in _RequestRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.RequestID equals request.ID
						 into requestGroup
						 from request in requestGroup.DefaultIfEmpty()

						 join sirket in _sirketRepo.GetAll2() on request.SirketID equals sirket.ID into sirketGroup
						 from sirket in sirketGroup.DefaultIfEmpty()

						 join durum in _RequestDurumRepo.GetAll2() on request.DurumID equals durum.ID into durumGroup
						 from durum in durumGroup.DefaultIfEmpty()

						 join OfferDurum in _OfferDurumRepo.GetAll2() on kayit.OfferDurumID equals OfferDurum.ID
						  into OfferDurumGroup
						 from OfferDurum in OfferDurumGroup.DefaultIfEmpty()

						 join offer in _OfferRepo.GetAll2() on kayit.ID equals offer.RequestUrunID
						 into offerGroup
						 from offer in offerGroup.DefaultIfEmpty()
						 where offer.IsSelected == true



						 join tur in _RequestTurRepo.GetAll2() on request.TurID equals tur.ID into turGroup
						 from tur in turGroup.DefaultIfEmpty()

						 join urunler in _urunlerRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.UrunID equals urunler.ID
						 into urunlerGroup
						 from urunler in urunlerGroup.DefaultIfEmpty()


						 join birim in _malzemeBirimRepo.GetAll2() on urunler.BirimID equals birim.ID
						 into birimGroup
						 from birim in birimGroup.DefaultIfEmpty()



						 join tip in _malzemeTipiRepo.GetAll2() on urunler.TipID equals tip.ID
						into tipGroup
						 from tip in tipGroup.DefaultIfEmpty()



						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new RequestUrunlerVM
						 {
							 ID = kayit.ID,
							 UrunAd = urunler.Ad != null ? urunler.Ad : "",
							 UrunKod = urunler.Kod != null ? urunler.Kod : "",

							 BirimID = urunler.BirimID,
							 BirimAd = birim.Ad,
							 TipAd = tip.Ad,
							 TipID = tip.ID,

							 Aciklama = kayit.Aciklama,
							 RequestID = kayit.RequestID,

							 UrunID = kayit.UrunID,
							 Miktar = (double)kayit.Miktar,
							 MiktarSon = (double)kayit.MiktarSon,
							 OnayliMi = kayit.OnayliMi,
							 RequestDate = request != null ? request.RequestDate : new DateTime(1000, 1, 1),
							 RequestDurumAd = durum != null ? durum.Ad : "",
							 RequestDurumID = (int)request.DurumID,
							 RequestDurumClass = durum != null ? durum.Class : "",
							 RequestTurID = (int)request.TurID,
							 RequestTurAd = tur != null ? tur.Ad : "",

							 OfferDurumID = kayit.OfferDurumID,
							 OfferDurumAd = OfferDurum.Ad,
							 DurumClass = OfferDurum.Class,

							 OfferID = offer != null ? offer.ID : 0,
							 SirketID = (int)request.SirketID,
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


		public async Task<bool> RequestUrunCikar(int urunId)
		{
			var model = _RequestUrunlerRepo.GetById(urunId);
			model.IsDelete = true;
			model.DeleteDate=DateTime.Now;
			model.DeleteUserID = _userId;
			await _RequestUrunlerRepo.UpdateAsync(model);
			// _RequestUrunlerRepo.Delete(model);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> RequestUrunDuzenle(RequestUrunlerVM model)
		{
			var urun = _RequestUrunlerRepo.GetById(model.ID);
						


			_mapper.Map(model, urun); 
			await _RequestUrunlerRepo.UpdateAsync(urun);
			 
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<RequestUrunlerVM> RequestUrunGetir(int UrunId)
		{
			return await  RequestUrunlerGenelListe().Where(p => p.ID == UrunId).FirstOrDefaultAsync();
		}

		public async  Task<int> RequestUrunDurum(int RequestID)
		{
			return  RequestUrunlerGenelListe().Where(p => p.OnayliMi == false && p.IsActive==true && p.IsDelete == false && p.RequestID == RequestID).Count();
		}

		public async Task<PagedResult<RequestUrunlerVM>> UrunListeleAsync(int page, int pageSize, CancellationToken ct = default,int offerDurumID = 0 )
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = RequestUrunlerGenelListe(); // IQueryable<RequestUrunlerVM>

				if (offerDurumID!=0)
				{
					query = query.Where(p => p.OfferDurumID == offerDurumID);
				}
				 

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<RequestUrunlerVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<RequestUrunlerVM>
				{
					Items = new List<RequestUrunlerVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<PagedResult<RequestVM>> TalepListeleAsync(int page, int pageSize, CancellationToken ct = default, int durumID = 0)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<RequestVM>

				if (durumID != 0)
				{
					query = query.Where(p => p.DurumID == durumID);
				}

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<RequestVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<RequestVM>
				{
					Items = new List<RequestVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}



		public async Task<PagedResult<RequestUrunlerVM>> OfferListeleAsync(int page, int pageSize, CancellationToken ct = default, int offerDurumID = 0)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = OfferGenelListe(); // IQueryable<RequestUrunlerVM>

				if (offerDurumID != 0)
				{
					query = query.Where(p => p.OfferDurumID == offerDurumID);
				}


				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<RequestUrunlerVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<RequestUrunlerVM>
				{
					Items = new List<RequestUrunlerVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<PagedResult<RequestVM>> TalepListeleAsync(RequestVM requestVM)
		{
			try
			{
				if (requestVM.PageIndex < 1) requestVM.PageIndex = 1;
				// mantıklı bir üst sınır koy
				if (requestVM.PageSize <= 0 || requestVM.PageSize > 1000) requestVM.PageSize = 50;

				var query = GenelListe(); // IQueryable<RequestVM>

				if (requestVM.DurumID != 0)
				{
					query = query.Where(p => p.DurumID == requestVM.DurumID);
				}
				if (!string.IsNullOrEmpty(requestVM.Aciklama))
				{
					query = query.Where(p => p.Aciklama.Contains(requestVM.Aciklama)
					 || p.DurumAd.Contains(requestVM.Aciklama)
					 || p.Not.Contains(requestVM.Aciklama) 
					 || p.SirketAd.Contains(requestVM.Aciklama)
					 );
				}
				if (requestVM.SirketID != 0)
				{
					query = query.Where(p => p.SirketID == requestVM.SirketID);
				}
				if (requestVM.TurID != 0)
				{
					query = query.Where(p => p.TurID == requestVM.TurID);
				}


				var total = await query.CountAsync(requestVM.ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((requestVM.PageIndex - 1) * requestVM.PageSize)
					.Take(requestVM.PageSize)
					.ToListAsync(requestVM.ct);

				return new PagedResult<RequestVM>
				{
					Items = items,
					PageIndex = requestVM.PageIndex,
					PageSize = requestVM.PageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<RequestVM>
				{
					Items = new List<RequestVM>(),
					PageIndex = requestVM.PageIndex,
					PageSize = requestVM.PageSize,
					TotalCount = 0
				};
			}
		}
	}
}
