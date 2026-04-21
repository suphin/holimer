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
	
		  
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
 

		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		 
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public OfferService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Offer> OfferRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<OfferDurum> OfferDurumRepo
			, IRepository<OfferTur> OfferTurRepo
		 , IRepository<Sirketler> sirketRepo 
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			 
		 
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
		  
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			 
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

						 select new OfferVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 DurumID = kayit.DurumID,
							 DurumAd = OfferDurum != null ? OfferDurum.Ad : "",
							 DurumClass = OfferDurum != null ? OfferDurum.Class : "",
							 SirketAd = sirket != null ? sirket.SirketAdi : "",
							 SirketID = kayit.SirketID,

							 TurID = kayit.TurID,
							 TurAd = OfferTur != null ? OfferTur.Ad : "",

							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
						 
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

			 

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama)

				);
			}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
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
