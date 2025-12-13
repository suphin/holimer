using AutoMapper;
using Azure;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.FilterVM;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class MalzemeService : IMalzemeService
	{
		private readonly ApplicationDbContext _context;
		private readonly LogoContext _logo;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IMapper _mapper;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IMemoryCache _cache;
		private const string CacheKey = "TumMalzemeListesi";
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Malzeme> _malzemeRepo;
		private readonly IRepository<MalzemeGrup> _grupRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
		
		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly IRepository<MalzemeFiyat> _malzemeFiyatRepo;
		private readonly IRepository<MalzemeDepo> _depoRepo;
		private readonly IRepository<MalzemeStok> _stokRepo;
		private readonly IRepository<MalzemeHareketTur> _hareketTurRepo;
		private readonly IRepository<Departman> _departmanRepo;
		

		public MalzemeService(IMapper mapper, ApplicationDbContext context,
			IHttpContextAccessor httpContextAccessor, IRepository<MalzemeGrup> grupRepo,
			IRepository<Kullanici> userRepo, IRepository<Malzeme> malzemeRepo,
			IRepository<MalzemeGrup> altGrupRepo, IRepository<MalzemeBirim> malzemeBirimRepo, IRepository<MalzemeStok> stokRepo, IRepository<MalzemeHareketTur> hareketTurRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo, IRepository<MalzemeFiyat> malzemeFiyatRepo, IRepository<MalzemeDepo> depoRepo, IRepository<Departman> departmanRepo,
			IRepository<DovizTur> dovizTurRepo,
			IWebHostEnvironment hostingEnvironment, IMemoryCache cache, LogoContext logo)
		{
			_httpContextAccessor = httpContextAccessor;
			_user = _httpContextAccessor.HttpContext.User;
			_userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			_cache = cache;
			_context = context;
			_logo = logo;
			_mapper = mapper;

			_grupRepo = grupRepo;
			_userRepo = userRepo;
			_malzemeRepo = malzemeRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			_malzemeFiyatRepo = malzemeFiyatRepo;
			_depoRepo = depoRepo;
			_departmanRepo = departmanRepo;
			_stokRepo = stokRepo;
			_hareketTurRepo = hareketTurRepo;
			_hostingEnvironment = hostingEnvironment;
			_dovizTurRepo = dovizTurRepo;
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
		public IQueryable<MalzemelerVM> GenelListe()
		{
			var result = from malzeme in _malzemeRepo.GetAll2()

						 join altGrup in _altGrupRepo.GetAll2() on malzeme.GrupID equals altGrup.ID
						 into altGrupGroup
						 from altGrup in altGrupGroup.DefaultIfEmpty()

						 join grup in _altGrupRepo.GetAll2() on altGrup.ParentID equals grup.ID
						 into grupGroup
						 from grup in grupGroup.DefaultIfEmpty()

						 join birim in _malzemeBirimRepo.GetAll2() on malzeme.BirimID equals birim.ID
						 into birimGroup
						 from birim in birimGroup.DefaultIfEmpty()

						 join doviz in _dovizTurRepo.GetAll2() on malzeme.DovizTur equals doviz.ID
						 into dovizGroup
						 from doviz in dovizGroup.DefaultIfEmpty()

						 join tip in _malzemeTipiRepo.GetAll2() on malzeme.TipID equals tip.ID
						into tipGroup
						 from tip in tipGroup.DefaultIfEmpty()


						 join user in _userRepo.GetAll2() on malzeme.CreateUserID equals user.Id
						 into userGroup
						 from user in userGroup.DefaultIfEmpty()


						 select new MalzemelerVM
						 {
							 ID = malzeme.ID,
							 Ad = malzeme.Ad != null ? malzeme.Ad : "",
							 Kod = malzeme.Kod != null ? malzeme.Kod : "",
							 Marka = malzeme.Marka ?? "",
							 Model = malzeme.Model ?? "",
							 Aciklama = malzeme.Aciklama != null ? malzeme.Aciklama : "",
							 Grup = grup.Ad,
							 AltGrup = altGrup.Ad,
							 GrupID = grup.ID,
							 AltGrupID = altGrup.ID,
							 BirimID = malzeme.BirimID,
							 BirimAd = birim.Ad,
							 TipAd = tip.Ad,
							 TipID = tip.ID,
							 KritikMiktar = malzeme.KritikMiktar,
							 CreateUserID = malzeme.CreateUserID,
							 CreateUserName = user != null ? user.AdSoyad : "",
							 CreateDate = malzeme.CreateDate != null ? malzeme.CreateDate : new DateTime(1000, 1, 1),
							 Fotograf = malzeme.Fotograf ?? "",
							 Fiyat = malzeme.Fiyat,
							 Kdv = malzeme.Kdv,
							 Indirim = malzeme.Indirim,
							 DovizTur = malzeme.DovizTur,
							 DovizTurAd = doviz.Ad,
							 MaliyetSatis = malzeme.MaliyetSatis,
							 Maliyet = malzeme.Maliyet,
							 SonMaliyetGuncellemeTarih = malzeme.SonMaliyetGuncellemeTarih,
							  SonFiyatGuncellemeTarih = malzeme.SonFiyatGuncellemeTarih,

							 IsActive = (bool)malzeme.IsActive,
							 IsDelete = (bool)malzeme.IsDelete,


						 };



			return result;
		}

		public Task<MalzemelerVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(MalzemelerVM model)
		{
			 
			Malzeme? existingEntry = _malzemeRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Malzeme>(model);
				_malzemeRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_malzemeRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			_cache.Remove(CacheKey);
			return true;
		}
		public async Task<bool> VeriEkleAsync(MalzemelerVM model)
		{
			 
			Malzeme? existingEntry = _malzemeRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Malzeme>(model);
				_malzemeRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _malzemeRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			_cache.Remove(CacheKey);
			return true;
		}

		public async Task<MalzemelerVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new MalzemelerVM();
			}

			MalzemelerVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new MalzemelerVM();
			}

			return kayit;
		}
		public async Task<List<MalzemelerVM>> VeriListele(MalzemelerVM model)
		{

			List<TagifyDto> grupList = null;

			// Tagify alanı doluysa parse et
			if (!string.IsNullOrWhiteSpace(model.tagifyGruplar))
			{
				grupList = JsonSerializer.Deserialize<List<TagifyDto>>(model.tagifyGruplar);
			}

			// ID listesi doluysa dolu olsun, değilse boş liste olsun
			var idList = (grupList != null)
				? grupList.Select(x => int.Parse(x.Id)).ToList()
				: new List<int>();

			var liste = GenelListe();

			if (idList.Any())
			{
				liste = liste.Where(x => idList.Contains((int)x.AltGrupID));
			}

			//if (model.Aciklama != null)
			if (!string.IsNullOrWhiteSpace(model.Aciklama))
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.Ad.Contains(model.Aciklama) ||
				p.Kod.Contains(model.Aciklama)
				);
			}
			//if (model.GrupID != 0)
			//{
			//	liste = liste.Where(p =>p.AltGrupID==model.GrupID);
			//}


			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}
		public async Task<PagedResult<MalzemelerVM>> VeriListeleAsync(MalzemelerVM modelv)
		{			 
			try
			{
				
				if (modelv.PageIndex < 1) modelv.PageIndex = 1;
				// mantıklı bir üst sınır koy
				if (modelv.PageSize <= 0 || modelv.PageSize > 1000) modelv.PageSize = 10;

				var query = GenelListe();



				if (modelv.Aciklama != null)
				{
					query = query.Where(p => p.Aciklama.Contains(modelv.Aciklama) ||
					p.Ad.Contains(modelv.Aciklama)
					);
				}

				var total = await query.CountAsync(modelv.ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((modelv.PageIndex - 1) * modelv.PageSize)
					.Take(modelv.PageSize)
					.ToListAsync(modelv.ct);

				return new PagedResult<MalzemelerVM>
				{
					Items = items,
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<MalzemelerVM>
				{
					Items = new List<MalzemelerVM>(),
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<List<MalzemelerVM>> VeriListele()
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
			Malzeme? kayit = _malzemeRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _malzemeRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
				_cache.Remove(CacheKey);
			}
			return true;
		}

		public async Task<PagedResult<MalzemelerVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<MalzemelerVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderBy(a => a.Kod)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<MalzemelerVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<MalzemelerVM>
				{
					Items = new List<MalzemelerVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}
		public async Task<PagedResult<MalzemelerVM>> VeriListeleAsync(
	int page, int pageSize, MalzemelerFilterVM f, CancellationToken ct = default)
		{
			IQueryable<MalzemelerVM> q = GenelListe();
			 
			if (f.GrupID.HasValue) q = q.Where(a => a.GrupID == f.AltGrupID); 
			if (!string.IsNullOrWhiteSpace(f.Aciklama))
				q = q.Where(a => a.Ad.Contains(f.Aciklama) || a.Kod.Contains(f.Aciklama) || a.Aciklama.Contains(f.Aciklama));

			q = q.OrderByDescending(a => a.ID);

			var total = await q.CountAsync(ct);
			var items = await q.Skip((page - 1) * pageSize)
							   .Take(pageSize) 
							   .ToListAsync(ct);

			return new PagedResult<MalzemelerVM>
			{
				Items = items,
				PageIndex = page,
				PageSize = pageSize,
				TotalCount = total
			};
		}

		public async Task<List<MalzemelerVM>> MalzemeAra(string malzemeAd)
		{
			//var results = await  GenelListe().Where(p => p.Ad.Contains(malzemeAd)).ToListAsync();
			//return results;
			var tumListe = await GetAllCachedAsync();
			return tumListe
				.Where(p => p.Ad.Contains(malzemeAd, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}
		public async Task<List<MalzemelerVM>> Malzemeler()
		{
			//var results = await  GenelListe().Where(p => p.Ad.Contains(malzemeAd)).ToListAsync();
			//return results;
			var tumListe = await GetAllCachedAsync();
			return tumListe
				.Where(p => p.IsActive==true && p.IsDelete==false)
				.ToList();
		}

		private async Task<List<MalzemelerVM>> GetAllCachedAsync()
		{
			return await _cache.GetOrCreateAsync(CacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
				// veritabanından bir kez çek
				var list = await GenelListe().ToListAsync();
				return list;
			});
		}

	 
		public async Task<bool> LogoMalzemeAktar()
		{
			// 1) Logo stoklarını çek
			//var logoItems = await _logo.Items
			//	.Where(x => x.ACTIVE == 0)
			//	.ToListAsync();

			//foreach (var item in logoItems)
			//{
			//	// 2) Portalda kod var mı
			//	var mevcut = await _context.Malzeme
			//		.FirstOrDefaultAsync(x => x.Kod == item.CODE);

			//	if (mevcut == null)
			//	{
			//		// 3) Yeni ekle
			//		var yeni = new Malzeme
			//		{
			//			Kod = item.CODE,
			//			Ad = item.NAME,
			//			Aciklama = item.SPECODE,
			//			Marka = item.PRODUCERCODE,
			//			Model = item.CYPHCODE,
			//			Kdv = item.VAT,
			//			CreateDate = DateTime.Now,
			//			IsActive = true,
			//			IsDelete=false,
			//			CreateUserID=_userId,
			//			GrupID=0,

			//		};

			//		await _context.Malzeme.AddAsync(yeni);
			//	}
			//	else
			//	{
			//		// 4) Güncelle
			//		mevcut.Ad = item.NAME;
			//		mevcut.Aciklama = item.SPECODE;
			//		mevcut.Marka = item.PRODUCERCODE;
			//		mevcut.Model = item.CYPHCODE;
			//		mevcut.Kdv = item.VAT;
			//		mevcut.UpdateDate = DateTime.Now;
			//		mevcut.UpdateUserID = _userId;
			//	}
			//}
			var logoItems = await _logo.LogoItems.Where(p=>p.ProductCode.StartsWith("1")).ToListAsync();

			foreach (var item in logoItems)
			{
				// 1) Grup kodunu çıkart (son parçayı at: 150HM.01.01.001 -> 150HM.01.01)
				string kod = item.ProductCode ?? "";
				string grupKod = "";

				if (!string.IsNullOrEmpty(kod))
				{
					var parts = kod.Split('.');
					if (parts.Length >= 3)
					{
						// Son parçayı atıyoruz
						grupKod = string.Join(".", parts[..^1]);
					}
				}

				int grupId = 0;

				if (!string.IsNullOrEmpty(grupKod))
				{
					// 2) Aynı grup kodu ile başlayan bir malzeme bul
					var grupUrun = await _context.Malzeme
						.FirstOrDefaultAsync(x => x.Kod.StartsWith(grupKod));

					if (grupUrun != null)
					{
						grupId = grupUrun.GrupID;
					}
				}

				// 3) Portalda kod var mı
				var mevcut = await _context.Malzeme
					.FirstOrDefaultAsync(x => x.Kod == item.ProductCode);

				if (mevcut == null)
				{
					// 4) Yeni ekle
					var yeni = new Malzeme
					{
						Kod = item.ProductCode,
						Ad = item.ProductName,
						Aciklama = item.ProductName2,
						Marka = item.ProducerCode,
						Model = item.Model,
						Kdv = item.VAT,
						CreateDate = DateTime.Now,
						IsActive = true,
						IsDelete = false,
						CreateUserID = _userId,
						BirimID=1,
						TipID=1,
						GrupID = grupId,
						BarkodNo=item.Barcode
					};

					await _context.Malzeme.AddAsync(yeni);
				}
				else
				{
					// 5) Güncelle
					mevcut.Ad = item.ProductName;
					mevcut.Aciklama = item.ProductName2;
					mevcut.Marka = item.ProducerCode;
					mevcut.Model = item.Model;
					mevcut.Kdv = item.VAT;
					mevcut.UpdateDate = DateTime.Now;
					mevcut.UpdateUserID = _userId;
					mevcut.BarkodNo = item.Barcode;

					// Gruplandırmayı da güncellemek istersen (isteğe bağlı)
					//mevcut.GrupID = grupId;
				}
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<List<MalzemeFiyat>> FiyatGetir(int UrunID)
		{
			try
			{
				List<MalzemeFiyat> fiyatlar = await _malzemeFiyatRepo.GetAll2().Where(p => p.MalzemeID == UrunID).ToListAsync();
				if (fiyatlar == null)
				{
					return new List<MalzemeFiyat>();
				}
				return fiyatlar;
			}
			catch (Exception ex)
			{

				return new List<MalzemeFiyat>(); ;
			}
		}

		public async Task<bool> FiyatDegistir(MalzemeFiyat model)
		{
			try
			{
				var urun = await _malzemeRepo.GetByIdAsync(model.MalzemeID);
				if (urun != null)
				{
					urun.Fiyat = model.Fiyat;
					urun.Maliyet = model.Maliyet;
					urun.DovizTur = model.DovizTur;
					_malzemeRepo.Update(urun);
					await _context.SaveChangesAsync();
				}
				var fiyat = new MalzemeFiyat();
				fiyat.Aciklama = model.Aciklama;
				fiyat.MalzemeID = model.MalzemeID;
				fiyat.Fiyat = model.Fiyat;
				fiyat.Maliyet = model.Maliyet;
				fiyat.CreateDate = DateTime.Now;
				fiyat.Tarih = DateTime.Now;
				fiyat.IsActive = true;
				fiyat.IsDelete = false;
				fiyat.CreateUserID = _userId;
				fiyat.DovizTur = model.DovizTur;

				_malzemeFiyatRepo.Update(fiyat);
				await _context.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{

				return false;
			}
		}

	}
}
