 
 
 
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
	public class SiparisService : ISiparisService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Siparis> _SiparisRepo;
		private readonly IRepository<SiparisDurum> _SiparisDurumRepo;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<SiparisUrunler> _SiparisUrunlerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
		private readonly IRepository<SiparisIskonto> _iskontoRepo;

		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public SiparisService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Siparis> SiparisRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<SiparisDurum> SiparisDurumRepo
			, IRepository<Sehirler> sehirRepo
			, IRepository<Musteriler> musterilerRepo
			, IRepository<SiparisUrunler> SiparisUrunlerRepo
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			, IRepository<DovizTur> dovizTurRepo
			, IRepository<SiparisIskonto> iskontoRepo
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_SiparisRepo = SiparisRepo;
			_iskontoRepo=iskontoRepo;
			_userRepo = userRepo;
			_SiparisDurumRepo = SiparisDurumRepo;
			_sehirRepo = sehirRepo;
			_musterilerRepo = musterilerRepo;
			_SiparisUrunlerRepo = SiparisUrunlerRepo;
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

		public async Task<bool> SiparisAktar(List<Siparis> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			foreach (var model in liste)
			{
				_SiparisRepo.Add(model);
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

		public IQueryable<SiparisVM> GenelListe()
		{
			Expression<Func<Siparis, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _SiparisRepo.GetAll2(filter)


						 join SiparisDurum in _SiparisDurumRepo.GetAll2() on kayit.DurumID equals SiparisDurum.ID
						 into SiparisDurumGroup
						 from SiparisDurum in SiparisDurumGroup.DefaultIfEmpty()

						 join musteriler in _musterilerRepo.GetAll2() on kayit.MusteriID equals musteriler.ID
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

						 select new SiparisVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 DurumID = kayit.DurumID,
							 DurumAd = SiparisDurum != null ? SiparisDurum.Ad : "",
							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
							 MusteriID = kayit.MusteriID,
							 GorevliID = kayit.GorevliID,							 
							 TeklifID = kayit.TeklifID,
							 SorumluID= kayit.SorumluID,
							 SorumluAd= sorumlu != null ? sorumlu.AdSoyad : "",
							 Musteri = musteriler,
                             BrutToplam = (double)kayit.BrutToplam,
							 KdvToplam = (double)kayit.KdvToplam,
							 IskontoToplam = (double)kayit.IskontoToplam,
							DolarKuru = (double)kayit.DolarKuru,
							EuroKuru = (double)kayit.EuroKuru,
							FirsatID= kayit.FirsatID,
							 IsLocked = (bool)kayit.IsLocked,
							 Toplam= kayit.Toplam,
							 SatirIskontoToplam= (double)kayit.SatirIskontoToplam,
							 TeslimTarihi= kayit.TeslimTarihi,
							 TeslimAdres= kayit.TeslimAdres,
							 
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



		public Task<SiparisVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(SiparisVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Siparis? existingEntry = _SiparisRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Siparis>(model);
				_SiparisRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_SiparisRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(SiparisVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Siparis? existingEntry = _SiparisRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Siparis>(model);
				_SiparisRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _SiparisRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<int> VeriEkleReturnIDAsync(SiparisVM model)
		{
			model.Not = (model.Not ?? string.Empty).Replace("\r\n", "");

			Siparis entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _SiparisRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Siparis {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _SiparisRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Siparis>(model);

				// Tercihen async ekleme
				await _SiparisRepo.AddAsync(entity);
				// Eğer Add async değilse: _SiparisRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}

		public async Task<SiparisVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new SiparisVM();
			}

			SiparisVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new SiparisVM();
			}

			return kayit;
		}

		public async Task<List<SiparisVM>> VeriListele(SiparisVM model)
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

		public async Task<List<SiparisVM>> VeriListele()
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
			Siparis? kayit = _SiparisRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _SiparisRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<SiparisVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<SiparisVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<SiparisVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<SiparisVM>
				{
					Items = new List<SiparisVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<SiparisVM>> VeriListeleAsync(SiparisVM model)
		{
			throw new NotImplementedException();
		}


		public async Task<bool> SiparisUrunEkle(SiparisUrunlerVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			 
				var model = new SiparisUrunler
				{
					UrunID = modelv.UrunID,
					SiparisID = modelv.SiparisID,
					TeklifID = modelv.TeklifID,
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
				_SiparisUrunlerRepo.Add(model);
			 

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<SiparisUrunlerVM>> SiparisUrunlerGetir(int SiparisID)
		{
			if (SiparisID!=0)
			{
				try
				{
					var list = await SiparisUrunlerGenelListe().Where(p => p.SiparisID == SiparisID).ToListAsync();

					return list;
				}
				catch (Exception ex)
				{

					return new List<SiparisUrunlerVM>();
				}
				 

				
			}
			else
			{
				return new List<SiparisUrunlerVM>();
			}
		}


		public IQueryable<SiparisUrunlerVM> SiparisUrunlerGenelListe()
		{
			Expression<Func<SiparisUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _SiparisUrunlerRepo.GetAll2(filter)


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

						 select new SiparisUrunlerVM
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
							 TeklifID = kayit.TeklifID,
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

		public async Task<bool> SiparisUrunCikar(int urunId)
		{
			var model = _SiparisUrunlerRepo.GetById(urunId);
			model.IsDelete = true;
			model.DeleteDate=DateTime.Now;
			model.DeleteUserID = _userId;
			await _SiparisUrunlerRepo.UpdateAsync(model);
			// _SiparisUrunlerRepo.Delete(model);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<SiparisIskonto>> SiparisIskontoGetir(int SiparisID)
		{
			if (SiparisID != 0)
			{
				try
				{
					var list = await  _iskontoRepo.GetAll2(p=>p.SiparisID == SiparisID).ToListAsync();

					return list;
				}
				catch (Exception ex)
				{

					return new List<SiparisIskonto>();
				}



			}
			else
			{
				return new List<SiparisIskonto>();
			}
		}

		public async Task<bool> SiparisIskontoEkle(SiparisIskonto modelv)
		{
			var newGuid = Guid.NewGuid().ToString();

			var model = new SiparisIskonto
			{
				 Ad=modelv.Ad,
				 Aciklama=modelv.Aciklama,
				 Oran=modelv.Oran,
				 SiparisID=modelv.SiparisID,

				CreateDate = DateTime.Now,
				IsActive = true,
				IsDelete = false,
				CreateUserID = _userId,
				DosyaID = newGuid
			};
			_iskontoRepo.Add(model);


			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> SiparisIskontoCikar(int iskontoId)
		{
			var model = _iskontoRepo.GetById(iskontoId);
		 
			  _iskontoRepo.Delete(model); 
			await _context.SaveChangesAsync();
			return true;
		}
	}
}
