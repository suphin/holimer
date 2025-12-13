using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class ReceteService : IReceteService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Recete> _receteRepo;
		private readonly IRepository<ReceteParametre> _receteParametreRepo;
		private readonly IRepository<ReceteParametreDeger> _receteParametreDegerRepo;
		private readonly IRepository<Uretim> _uretimRepo;
		private readonly IRepository<UretimUrunler> _uretimUrunlerRepo;
		private readonly IRepository<UretimParametreDeger> _uretimParametreDegerRepo;

		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<ReceteUrunler> _receteUrunlerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
		 

		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public ReceteService(
			ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Siparis> SiparisRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<Recete> receteRepo
			, IRepository<Sehirler> sehirRepo
			, IRepository<Musteriler> musterilerRepo
			, IRepository<ReceteUrunler> receteUrunlerRepo
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			, IRepository<DovizTur> dovizTurRepo 
			, IRepository<ReceteParametre> receteParametreRepo
			, IRepository<ReceteParametreDeger> receteParametreDegerRepo 
			, IRepository<Uretim> uretimRepo, IRepository<UretimUrunler> uretimUrunlerRepo, IRepository<UretimParametreDeger> uretimParametreDegerRepo)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_receteRepo = receteRepo;
			_userRepo = userRepo;
			_sehirRepo = sehirRepo;
			_musterilerRepo = musterilerRepo;
			_receteUrunlerRepo = receteUrunlerRepo;
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			_dovizTurRepo = dovizTurRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
			_receteParametreRepo = receteParametreRepo;
			_receteParametreDegerRepo = receteParametreDegerRepo;
			_uretimRepo = uretimRepo;
			_uretimUrunlerRepo = uretimUrunlerRepo;
			_uretimParametreDegerRepo = uretimParametreDegerRepo;
		}
		public IQueryable<ReceteVM> GenelListe()
		{
			Expression<Func<Recete, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _receteRepo.GetAll2(filter)

  

						 join urun in _urunlerRepo.GetAll2() on kayit.UrunID equals urun.ID
						 into urunGroup
						 from urun in urunGroup.DefaultIfEmpty()


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new ReceteVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 UrunID = kayit.UrunID,
							 UrunAd=urun.Ad,
							 UrunKod=urun.Kod,
							 Malzeme=urun,

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
		public async Task<bool> ReceteParametreEkle(int receteId, List<int> seciliParametreler)
		{
			// Mevcut parametre kayıtlarını al
			var mevcutParametreler = await _receteParametreDegerRepo.GetAll2()
				.Where(x => x.ReceteID == receteId)
				.ToListAsync();

			// Eğer hiç parametre seçilmemişse -> tüm mevcut kayıtları sil
			if (seciliParametreler == null || !seciliParametreler.Any())
			{
				foreach (var eski in mevcutParametreler)
				{
					  _receteParametreDegerRepo.Delete(eski);
				}

				await _context.SaveChangesAsync();
				return true;
			}

			// Yeni gelen parametrelerden mevcutta olmayanlar → EKLENECEK
			var mevcutIdListesi = mevcutParametreler.Select(x => x.ParametreID).ToList();
			var eklenecekler = seciliParametreler.Except(mevcutIdListesi).ToList();

			foreach (var parametreId in eklenecekler)
			{
				var yeni = new ReceteParametreDeger
				{
					ReceteID = receteId,
					ParametreID = parametreId,
					IsActive = true,
					IsDelete = false,
					CreateDate = DateTime.Now,
					CreateUserID = _userId
				};

				await  _receteParametreDegerRepo.AddAsync(yeni);
			}

			// Mevcutta olup yeni listede olmayanlar → SİLİNECEK
			var silinecekler = mevcutParametreler
				.Where(x => !seciliParametreler.Contains(x.ParametreID))
				.ToList();

			foreach (var sil in silinecekler)
			{
				  _receteParametreDegerRepo.Delete(sil);
			}

			await _context.SaveChangesAsync();
			return true;
		}


		//public async Task<bool> ReceteParametreEkle(int ReceteID, List<int> SeciliParametreler)
		//{
		//	if (SeciliParametreler != null && SeciliParametreler.Any())
		//	{
		//		foreach (var parametreId in SeciliParametreler)
		//		{
		//			var paramValue = new ReceteParametreDeger
		//			{
		//				ReceteID = ReceteID,
		//				ParametreID = parametreId,
		//				IsActive=true,
		//				IsDelete=false,
		//				CreateDate=DateTime.Now,
		//				CreateUserID=_userId

		//			};
		//			_receteParametreDegerRepo.Add(paramValue);
		//		}
		//		await _context.SaveChangesAsync();
		//		return true;
		//	} else
		//		return false;
		//}

		public Task<List<ReceteParametreDeger>> ReceteParametreGetir(int ReceteID)
		{
			return _context.ReceteParametreDeger.Where(p => p.ReceteID == ReceteID).ToListAsync();
		}

		public async Task<bool> ReceteUrunCikar(int urunId)
		{
			var model = _receteUrunlerRepo.GetById(urunId);

			_receteUrunlerRepo.Delete(model);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ReceteUrunEkle(ReceteUrunlerVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();

			var model = new ReceteUrunler
			{
				UrunID = modelv.UrunID, 
				Fiyat = modelv.Fiyat,
				Kdv = modelv.Kdv,
				Iskonto = modelv.Iskonto,
				DovizTur = modelv.DovizTur,
				DovizTurAd = modelv.DovizTurAd,
				Miktar = modelv.Miktar,
				ReceteID=modelv.ReceteID,
				
				CreateDate = DateTime.Now,
				IsActive = true,
				IsDelete = false,
				CreateUserID = _userId,
				DosyaID = newGuid
			};
			_receteUrunlerRepo.Add(model);


			await _context.SaveChangesAsync();
			return true;
		}

	 
		public IQueryable<ReceteUrunlerVM> ReceteUrunlerGenelListe()
		{
			Expression<Func<ReceteUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _receteUrunlerRepo.GetAll2(filter)


						 join urunler in _urunlerRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.UrunID equals urunler.ID
						 into urunlerGroup
						 from urunler in urunlerGroup.DefaultIfEmpty()


						 join birim in _malzemeBirimRepo.GetAll2() on urunler.BirimID equals birim.ID
						 into birimGroup
						 from birim in birimGroup.DefaultIfEmpty()

						 join doviz in _dovizTurRepo.GetAll2() on urunler.DovizTur equals doviz.ID
						 into dovizGroup
						 from doviz in dovizGroup.DefaultIfEmpty()

						 join tip in _malzemeTipiRepo.GetAll2() on urunler.TipID equals tip.ID
						into tipGroup
						 from tip in tipGroup.DefaultIfEmpty()

						 join altGrup in _altGrupRepo.GetAll2() on urunler.GrupID equals altGrup.ID
						into altGrupGroup
						 from altGrup in altGrupGroup.DefaultIfEmpty()

						 join grup in _altGrupRepo.GetAll2() on altGrup.ParentID equals grup.ID
						 into grupGroup
						 from grup in grupGroup.DefaultIfEmpty()


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new ReceteUrunlerVM
						 {
							 ID = kayit.ID,
							 ReceteID=kayit.ReceteID,
							 UrunAd = urunler.Ad != null ? urunler.Ad : "",
							 UrunKod = urunler.Kod != null ? urunler.Kod : "",
							 UrunID = kayit.UrunID,
							 
							 BirimID = urunler.BirimID,
							 BirimAd = birim.Ad,
							 TipAd = tip.Ad,
							 TipID = tip.ID,

							 Fiyat = (double)kayit.Fiyat,
							 Kdv = (double)kayit.Kdv,
							 Iskonto = (double)kayit.Iskonto,
							 DovizTur = urunler.DovizTur,
							 DovizTurAd = doviz.Ad,

							 Grup = grup.Ad,
							 AltGrup = altGrup.Ad,
							 GrupID = grup.ID,
							 AltGrupID = altGrup.ID,

							 Miktar = (double)kayit.Miktar, 
							 

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
		public async Task<List<ReceteUrunlerVM>> ReceteUrunlerGetir(int ReceteID)
		{
			if (ReceteID != 0)
			{
				try
				{
					var list = await ReceteUrunlerGenelListe().Where(p => p.ReceteID == ReceteID).ToListAsync();

					return list;
				}
				catch (Exception ex)
				{

					return new List<ReceteUrunlerVM>();
				}



			}
			else
			{
				return new List<ReceteUrunlerVM>();
			}
		}

		public async Task<bool> UretimEkle(UretimVM model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			// Transaction başlat
			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					// 1️⃣ Ana kayıt
					var uretim = new Uretim
					{
						SiparisTarihi = model.SiparisTarihi,
						PartiNo = model.PartiNo,
						TerminSuresi = model.TerminSuresi,
						TeslimTarihi = model.TeslimTarihi,
						UreticiID = model.UreticiID,
						GerceklesenMiktar=model.GerceklesenMiktar,
						HesaplananMiktar=model.HesaplananMiktar,
						CreateDate = DateTime.UtcNow,
						CreateUserID=_userId,
						IsActive = true,
						IsDelete=false,
						UrunID=model.UrunID,
						ReceteID=model.ReceteID

					};

					await _uretimRepo.AddAsync(uretim);
					await _context.SaveChangesAsync();

					// 2️⃣ Ürünler
					if (model.UretimUrunler != null && model.UretimUrunler.Any())
					{
						foreach (var urun in model.UretimUrunler)
						{
							var entity = new UretimUrunler
							{
								UretimID = uretim.ID,
								UrunID = urun.UrunID,
								Deger = urun.Deger,
								CreateDate = DateTime.UtcNow,
								CreateUserID = _userId,
								IsActive = true,
								IsDelete = false,

							};
							await _uretimUrunlerRepo.AddAsync(entity);
						}
					}

					// 3️⃣ Parametreler
					if (model.UretimParametreDeger != null && model.UretimParametreDeger.Any())
					{
						foreach (var param in model.UretimParametreDeger)
						{
							var entity = new UretimParametreDeger
							{
								UretimID = uretim.ID,
								ParametreID= param.ParametreID,
								Deger = param.Deger,
								CreateDate = DateTime.UtcNow,
								CreateUserID = _userId,
								IsActive = true,
								IsDelete = false,
							};
							await _uretimParametreDegerRepo.AddAsync(entity);
						}
					}

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();

					return true;
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					// Loglama yapabilirsin
					throw new Exception("Üretim eklenirken hata oluştu: " + ex.Message, ex);
				}
			}
		}

		public async Task<bool> UretimParametreEkle(List<UretimParametreDegerVM> model)
		{
			foreach (var param in model)
			{
				// ParametreValue tablosuna kaydet
				//var mevcut = _uretimParametreDegerRepo.GetAll(x => x.ReceteId == model.Id && x.ParametreId == param.ParametreId);

				//if (mevcut != null)
				//{
				//	mevcut.Deger = param.Deger;
				//	_parametreValueRepository.Update(mevcut);
				//}
				//else
				//{
				//	var yeni = new ParametreValue
				//	{
				//		ReceteId = model.Id,
				//		ParametreId = param.ParametreId,
				//		Deger = param.Deger
				//	};
				//	_parametreValueRepository.Insert(yeni);
				//}
			
			}
			return true;
		}

		public Task<bool> UretimParametreEkle(UretimParametreDegerVM model)
		{
			throw new NotImplementedException();
		}

		public Task<ReceteVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(ReceteVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Recete? existingEntry = _receteRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Recete>(model);
				_receteRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_receteRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(ReceteVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Recete? existingEntry = _receteRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Recete>(model);
				_receteRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _receteRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<int> VeriEkleReturnIDAsync(ReceteVM model)
		{
			model.Not = (model.Not ?? string.Empty).Replace("\r\n", "");

			Recete entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _receteRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Recete {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _receteRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Recete>(model);

				// Tercihen async ekleme
				await _receteRepo.AddAsync(entity);
				// Eğer Add async değilse: _SiparisIadeRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}


		public async Task<ReceteVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new ReceteVM();
			}

			ReceteVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new ReceteVM();
			}

			return kayit;
		}

		public async Task<ReceteVM> VeriGetirUrunID(int urunID)
		{
			if (urunID <= 0)
			{
				return new ReceteVM();
			}

			ReceteVM kayit = GenelListe().Where(p => p.UrunID == urunID).FirstOrDefault();
			if (kayit == null)
			{
				return new ReceteVM();
			}

			return kayit;
		}

		public async Task<List<ReceteVM>> VeriListele(ReceteVM model)
		{
			var liste = GenelListe();

			if (model.UrunID != null && model.UrunID > 0)
			{
				liste = liste.Where(p => p.UrunID == model.UrunID);
			}

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama)

				);
			}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<ReceteVM>> VeriListele()
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

		public async Task<PagedResult<ReceteVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<SiparisVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderBy(a => a.UrunAd)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<ReceteVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<ReceteVM>
				{
					Items = new List<ReceteVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<ReceteVM>> VeriListeleAsync(ReceteVM model)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> VeriSil(int id)
		{
			Recete? kayit = _receteRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _receteRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}
	}
}
