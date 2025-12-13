 
 
 
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
	public class TeklifService : ITeklifService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Teklif> _TeklifRepo;
		private readonly IRepository<TeklifDurum> _TeklifDurumRepo;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<TeklifUrunler> _teklifUrunlerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
        private readonly IRepository<TeklifIskonto> _iskontoRepo;

        private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public TeklifService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Teklif> TeklifRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IRepository<TeklifDurum> TeklifDurumRepo
			, IRepository<Sehirler> sehirRepo
			, IRepository<Musteriler> musterilerRepo
			, IRepository<TeklifUrunler> teklifUrunlerRepo
			, IRepository<Malzeme> urunlerRepo
			, IRepository<MalzemeGrup> altGrupRepo
			, IRepository<MalzemeBirim> malzemeBirimRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo
			, IRepository<DovizTur> dovizTurRepo
			, IRepository<TeklifIskonto> iskontoRepo

            )
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_TeklifRepo = TeklifRepo;
			_userRepo = userRepo;
			_TeklifDurumRepo = TeklifDurumRepo;
			_sehirRepo = sehirRepo;
			_musterilerRepo = musterilerRepo;
			_teklifUrunlerRepo = teklifUrunlerRepo;
			_urunlerRepo = urunlerRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			_dovizTurRepo = dovizTurRepo; 
			_iskontoRepo=iskontoRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
		}

		public async Task<bool> TeklifAktar(List<Teklif> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			foreach (var model in liste)
			{
				_TeklifRepo.Add(model);
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
		public IQueryable<TeklifVM> GenelListe()
		{
			Expression<Func<Teklif, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _TeklifRepo.GetAll2(filter)


						 join TeklifDurum in _TeklifDurumRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.DurumID equals TeklifDurum.ID
						 into TeklifDurumGroup
						 from TeklifDurum in TeklifDurumGroup.DefaultIfEmpty()

						 join musteriler in _musterilerRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on kayit.MusteriID equals musteriler.ID
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

						 select new TeklifVM
						 {
							 ID = kayit.ID,
							 Aciklama = kayit.Aciklama,
							 Not = kayit.Not,
							 DurumID = kayit.DurumID,
							 DurumAd = TeklifDurum != null ? TeklifDurum.Ad : "",
							 IsDone = (bool)kayit.IsDone,
							 TarihSaat = kayit.TarihSaat != null ? kayit.TarihSaat : new DateTime(1000, 1, 1),
							 MusteriID = kayit.MusteriID,
							 GorevliID = kayit.GorevliID,							 
							 SiparisID = kayit.SiparisID,
							 SorumluID= kayit.SorumluID,
							 SorumluAd= sorumlu != null ? sorumlu.AdSoyad : "",
							 Musteri = musteriler,
							BrutToplam = (double)kayit.BrutToplam,
							 KdvToplam = (double)kayit.KdvToplam,
							 IskontoToplam = (double)kayit.IskontoToplam,
                             Toplam = kayit.Toplam,
                             SatirIskontoToplam = (double)kayit.SatirIskontoToplam,


                             DolarKuru = (double)kayit.DolarKuru,
							EuroKuru = (double)kayit.EuroKuru,
							FirsatID= kayit.FirsatID,
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



		public Task<TeklifVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(TeklifVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Teklif? existingEntry = _TeklifRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Teklif>(model);
				_TeklifRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_TeklifRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(TeklifVM model)
		{
			model.Not = model.Not.Replace("\r\n", "");
			Teklif? existingEntry = _TeklifRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Teklif>(model);
				_TeklifRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _TeklifRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<int> VeriEkleReturnIDAsync(TeklifVM model)
		{
			model.Not = (model.Not ?? string.Empty).Replace("\r\n", "");

			Teklif entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _TeklifRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Teklif {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _TeklifRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Teklif>(model);

				// Tercihen async ekleme
				await _TeklifRepo.AddAsync(entity);
				// Eğer Add async değilse: _TeklifRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}

		public async Task<TeklifVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new TeklifVM();
			}

			TeklifVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new TeklifVM();
			}

			return kayit;
		}

		public async Task<List<TeklifVM>> VeriListele(TeklifVM model)
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

		public async Task<List<TeklifVM>> VeriListele()
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
			Teklif? kayit = _TeklifRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _TeklifRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<PagedResult<TeklifVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<TeklifVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<TeklifVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<TeklifVM>
				{
					Items = new List<TeklifVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<TeklifVM>> VeriListeleAsync(TeklifVM model)
		{
			throw new NotImplementedException();
		}


		public async Task<bool> TeklifUrunEkle(TeklifUrunlerVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			 
				var model = new TeklifUrunler
				{
					UrunID = modelv.UrunID,
					TeklifID = modelv.TeklifID,
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
				_teklifUrunlerRepo.Add(model);
			 

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<TeklifUrunlerVM>> TeklifUrunlerGetir(int teklifID)
		{
			if (teklifID!=0)
			{
				try
				{
					var list = await TeklifUrunlerGenelListe().Where(p => p.TeklifID == teklifID).ToListAsync();

					return list;
				}
				catch (Exception ex)
				{

					return new List<TeklifUrunlerVM>();
				}
				 

				
			}
			else
			{
				return new List<TeklifUrunlerVM>();
			}
		}


		public IQueryable<TeklifUrunlerVM> TeklifUrunlerGenelListe()
		{
			Expression<Func<TeklifUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _teklifUrunlerRepo.GetAll2(filter)

						

						 join urunler in _urunlerRepo.GetAll2() on kayit.UrunID equals urunler.ID
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

						 select new TeklifUrunlerVM
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
							 TeklifID = kayit.TeklifID,
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

		public async Task<bool> TeklifUrunCikar(int urunId)
		{
			var model = _teklifUrunlerRepo.GetById(urunId);
			model.IsDelete = true;
			model.DeleteDate=DateTime.Now;
			model.DeleteUserID = _userId;
			await _teklifUrunlerRepo.UpdateAsync(model);
			// _teklifUrunlerRepo.Delete(model);
			await _context.SaveChangesAsync();
			return true;
		}

        public async Task<List<TeklifIskonto>> TeklifIskontoGetir(int TeklifID)
        {
            if (TeklifID != 0)
            {
                try
                {
                    var list = await _iskontoRepo.GetAll2(p => p.TeklifID == TeklifID).ToListAsync();

                    return list;
                }
                catch (Exception ex)
                {

                    return new List<TeklifIskonto>();
                }



            }
            else
            {
                return new List<TeklifIskonto>();
            }
        }

        public async Task<bool> TeklifIskontoEkle(TeklifIskonto modelv)
        {
            var newGuid = Guid.NewGuid().ToString();

            var model = new TeklifIskonto
            {
                Ad = modelv.Ad,
                Aciklama = modelv.Aciklama,
                Oran = modelv.Oran,
                TeklifID = modelv.TeklifID,

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

        public async Task<bool> TeklifIskontoCikar(int iskontoId)
        {
            var model = _iskontoRepo.GetById(iskontoId);

            _iskontoRepo.Delete(model);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
