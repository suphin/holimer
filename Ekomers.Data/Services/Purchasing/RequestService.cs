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
		 
			_RequestUrunlerRepo = RequestUrunlerRepo;
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			 
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
	}
}
