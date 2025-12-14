 
 
 
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
	public class SatislarService : ISatislarService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Satislar> _SatislarRepo;
		private readonly IRepository<SatislarSebep> _SatislarSebepRepo;
		private readonly IRepository<SatislarDurum> _SatislarDurumRepo;
		private readonly IRepository<SatislarPlatform> _SatislarPlatformRepo;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<SatislarUrunler> _SatislarUrunlerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;

		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public SatislarService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Satislar> SatislarRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<SatislarDurum> SatislarDurumRepo
			,IRepository<SatislarSebep> SatislarSebepRepo
			,IRepository<SatislarPlatform> SatislarPlatformRepo
			, IRepository<Sehirler> sehirRepo
			, IRepository<Musteriler> musterilerRepo
			, IRepository<SatislarUrunler> SatislarUrunlerRepo
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			, IRepository<DovizTur> dovizTurRepo

			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_SatislarRepo = SatislarRepo;
			_SatislarSebepRepo = SatislarSebepRepo;
			_SatislarPlatformRepo = SatislarPlatformRepo;
			_userRepo = userRepo;
			_SatislarDurumRepo = SatislarDurumRepo;
			_sehirRepo = sehirRepo;
			_musterilerRepo = musterilerRepo;
			_SatislarUrunlerRepo = SatislarUrunlerRepo;
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			_dovizTurRepo = dovizTurRepo; 
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}

		public async Task<bool> SatislarAktar(List<Satislar> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			foreach (var model in liste)
			{
				_SatislarRepo.Add(model);
			}

			// Değişiklikleri veritabanına yansıt
			_context.SaveChanges();

			return true;
		}

		public async Task<IstatistikVM> VeriSayisi()
		{
			var liste=GenelListe();
			var model = new IstatistikVM
			{
				VeriSayisi = liste.Count(),
				AktifSayisi = liste.Where(p => p.IsActive == true).Count(),
				PasifSayisi = liste.Where(p => p.IsActive == false).Count(),
				SilinenSayisi = liste.Where(p => p.IsDelete == true).Count(),
				Toplam = liste.Sum(p => p.SiparisToplam)
			};
			return model;
		}

		public IQueryable<SatislarVM> GenelListe()
		{
			Expression<Func<Satislar, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _SatislarRepo.GetAll2(filter)


						 join SatislarDurum in _SatislarDurumRepo.GetAll2() on kayit.DurumID equals SatislarDurum.ID
						 into SatislarDurumGroup
						 from SatislarDurum in SatislarDurumGroup.DefaultIfEmpty()

						 join musteriler in _musterilerRepo.GetAll2() on kayit.MusteriID equals musteriler.ID
						 into musterilerGroup
						 from musteriler in musterilerGroup.DefaultIfEmpty()

						 join sorumlu in _userRepo.GetAll2() on kayit.SorumluID equals sorumlu.Id
						 into sorumluGroup
						 from sorumlu in sorumluGroup.DefaultIfEmpty()

						 join personel in _userRepo.GetAll2() on kayit.PersonelID equals personel.Id
						 into personelGroup
						 from personel in personelGroup.DefaultIfEmpty()

						

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new SatislarVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 DurumID = kayit.DurumID,
							 DurumAd = SatislarDurum != null ? SatislarDurum.Ad : "",
							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
							 MusteriID = kayit.MusteriID == null ? 0: kayit.MusteriID,
							 GorevliID = kayit.GorevliID,	
							 SorumluID= kayit.SorumluID,
							 SorumluAd= sorumlu != null ? sorumlu.AdSoyad : "",
							 Musteri = musteriler ?? new Musteriler(),
							 SiparisToplam = (double)kayit.SiparisToplam,
							 KdvToplam = (double)kayit.KdvToplam,
							 IskontoToplam = (double)kayit.IskontoToplam,
							DolarKuru = (double)kayit.DolarKuru,
							EuroKuru = (double)kayit.EuroKuru,
							 IsLocked = (bool)kayit.IsLocked,
							 PersonelAd=personel.AdSoyad,
							 PersonelID=kayit.PersonelID,
							 SiparisNo=kayit.SiparisNo,
							 CariTipi= kayit.CariTipi,
							 IsActive = (bool)kayit.IsActive,
							 IsDelete = (bool)kayit.IsDelete,
							 SiparisTarihi= kayit.SiparisTarihi,

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



		public Task<SatislarVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(SatislarVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Satislar? existingEntry = _SatislarRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Satislar>(model);
				_SatislarRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_SatislarRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(SatislarVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Satislar? existingEntry = _SatislarRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Satislar>(model);
				_SatislarRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _SatislarRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<int> VeriEkleReturnIDAsync(SatislarVM model)
		{
			model.Not = (model.Not ?? string.Empty).Replace("\r\n", "");

			Satislar entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _SatislarRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Satislar {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _SatislarRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Satislar>(model);

				// Tercihen async ekleme
				await _SatislarRepo.AddAsync(entity);
				// Eğer Add async değilse: _SatislarRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}

		public async Task<SatislarVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new SatislarVM();
			}

			SatislarVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new SatislarVM();
			}

			return kayit;
		}

		public async Task<List<SatislarVM>> VeriListele(SatislarVM model)
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

		public async Task<List<SatislarVM>> VeriListele()
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
			Satislar? kayit = _SatislarRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _SatislarRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<SatislarVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<SatislarVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<SatislarVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<SatislarVM>
				{
					Items = new List<SatislarVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<SatislarVM>> VeriListeleAsync(SatislarVM model)
		{
			throw new NotImplementedException();
		}


		public async Task<bool> SatislarUrunEkle(SatislarUrunlerVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			 
				var model = new SatislarUrunler
				{
					UrunID = modelv.UrunID,
					SiparisID = modelv.SiparisID,
					Fiyat = modelv.Fiyat,
					Kdv = modelv.Kdv,
					Iskonto = modelv.Iskonto,
					DovizTur= modelv.DovizTur,
					DovizTurAd= modelv.DovizTurAd,
					Miktar = modelv.Miktar,

					 
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId,
					DosyaID = newGuid
				};
				_SatislarUrunlerRepo.Add(model);
			 

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<SatislarUrunlerVM>> SatislarUrunlerGetir(int SatislarID)
		{
			if (SatislarID!=0)
			{
				try
				{
					var list = await SatislarUrunlerGenelListe().Where(p => p.SiparisID == SatislarID).ToListAsync();

					return list;
				}
				catch (Exception ex)
				{

					return new List<SatislarUrunlerVM>();
				}
				 

				
			}
			else
			{
				return new List<SatislarUrunlerVM>();
			}
		}


		public IQueryable<SatislarUrunlerVM> SatislarUrunlerGenelListe()
		{
			Expression<Func<SatislarUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _SatislarUrunlerRepo.GetAll2(filter)


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

						 

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new SatislarUrunlerVM
						 {
							 ID = kayit.ID,
							 UrunAd = urunler.Ad != null ? urunler.Ad : "",
							 UrunKod = urunler.Kod != null ? urunler.Kod : "",  
							 
							 BirimID = urunler.BirimID,
							 BirimAd = birim.Ad,
							 TipAd = tip.Ad,
							 TipID = tip.ID, 
							  
							 Fiyat = (double)kayit.Fiyat,
							 Kdv = (double)kayit.Kdv,
							 Iskonto = (double)kayit.Iskonto,
							 DovizTur = urunler.DovizTur,
							 DovizTurAd = doviz.Ad,
							 SiparisID = kayit.SiparisID,
							 UrunID = kayit.UrunID,
							 Miktar = (double)kayit.Miktar,
							 Toplam = (double)(kayit.Miktar * kayit.Fiyat),
							 //KdvTutar = (double)((kayit.Miktar * kayit.Fiyat) * (kayit.Kdv / 100)),
							 //IskontoTutar = (double)((kayit.Miktar * kayit.Fiyat) * (kayit.Iskonto / 100)),
							// GenelToplam = (double)((kayit.Miktar * kayit.Fiyat) + ((kayit.Miktar * kayit.Fiyat) * (kayit.Kdv / 100)) - ((kayit.Miktar * kayit.Fiyat) * (kayit.Iskonto / 100))),





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

		public async Task<bool> SatislarUrunCikar(int urunId)
		{
			var model = _SatislarUrunlerRepo.GetById(urunId);
			model.IsDelete = true;
			model.DeleteDate=DateTime.Now;
			model.DeleteUserID = _userId;
			await _SatislarUrunlerRepo.UpdateAsync(model);
			// _SatislarUrunlerRepo.Delete(model);
			await _context.SaveChangesAsync();
			return true;
		}
	}
}
