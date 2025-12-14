using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class UretimService : IUretimService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Recete> _receteRepo;
		private readonly IRepository<ReceteParametre> _receteParametreRepo;
		private readonly IRepository<ReceteParametreDeger> _receteParametreDegerRepo;
		private readonly IRepository<Uretici> _ureticiRepo;
		private readonly IRepository<Uretim> _uretimRepo;
		private readonly IRepository<UretimUrunler> _uretimUrunlerRepo;
		private readonly IRepository<UretimParametreDeger> _uretimParametreDegerRepo;

		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly IRepository<Musteriler> _musterilerRepo;
		private readonly IRepository<ReceteUrunler> _receteUrunlerRepo;
		private readonly IRepository<Malzeme> _urunlerRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
		private readonly IRepository<UretimTeslimat> _teslimatRepo;


		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public UretimService(
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
			,IRepository<Uretici> ureticiRepo
			, IRepository<UretimTeslimat> teslimatRepo
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
			_ureticiRepo = ureticiRepo;
			_teslimatRepo = teslimatRepo;
		}
		public IQueryable<UretimVM> GenelListe()
		{
			Expression<Func<Uretim, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _uretimRepo.GetAll2(filter)



						 join urun in _urunlerRepo.GetAll2() on kayit.UrunID equals urun.ID
						 into urunGroup
						 from urun in urunGroup.DefaultIfEmpty()

						 join uretici in _ureticiRepo.GetAll2() on kayit.UreticiID equals uretici.ID
						into ureticiGroup
						 from uretici in ureticiGroup.DefaultIfEmpty()


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new UretimVM
						 {
							 ID = kayit.ID, 
							 UrunID = kayit.UrunID,
							 UrunAd = urun.Ad,
							 UrunKod = urun.Kod,
							 Malzeme = urun,
							 PartiNo=kayit.PartiNo,
							 HesaplananMiktar=kayit.HesaplananMiktar,
							 GerceklesenMiktar=kayit.GerceklesenMiktar,
							 UreticiID=kayit.UreticiID,
							 Uretici=uretici.Ad,
							 SiparisTarihi=kayit.SiparisTarihi,
							 TerminSuresi=kayit.TerminSuresi,
							 TeslimTarihi=kayit.TeslimTarihi,
							 Not=kayit.Not,
							 KayipFireOran=Math.Round((double)kayit.KayipFireOran,2),
							 KayipFireMiktar=kayit.KayipFireMiktar,
							 ParasalDeger=kayit.ParasalDeger,
							 HmCarpan=kayit.HmCarpan,

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

		public async Task<bool> KismiTeslimatCikar(int teslimatID)
		{
			var model = _teslimatRepo.GetById(teslimatID);
			 
			 _teslimatRepo.Delete(model);
			
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> KismiTeslimatEkle(UretimTeslimat modelv)
		{
			var newGuid = Guid.NewGuid().ToString();

			var model = new UretimTeslimat
			{
				Tarih = modelv.Tarih,
				 
				Miktar = modelv.Miktar,
				 UretimID= modelv.UretimID,

				CreateDate = DateTime.Now,
				IsActive = true,
				IsDelete = false,
				CreateUserID = _userId,
				DosyaID = newGuid
			};
			_teslimatRepo.Add(model);


			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<List<UretimTeslimat>> KismiTeslimatGetir(int UretimID)
		{
			return await _teslimatRepo.GetAll2(p => p.UretimID == UretimID).ToListAsync();
		}

		public async Task<List<UretimParametreDeger>> UrerimParametreGetir(int UretimID)
		{
			//return _context.UretimParametreDeger.Where(p => p.UretimID == UretimID).ToListAsync();
			var query = from up in _context.UretimParametreDeger
						join rp in _context.ReceteParametre
							on up.ParametreID equals rp.ID // veya up.ParametreId olabilir
						where up.UretimID == UretimID
						select new UretimParametreDeger
						{
							ID = up.ID,
							UretimID = up.UretimID,
							ParametreAd = rp.Ad, // eşleşen tablodan alıyoruz
							Deger = up.Deger
						};

			return await query.ToListAsync();
		}

	 
		public IQueryable<UretimUrunlerVM> UretimUrunlerGenelListe()
		{
			Expression<Func<UretimUrunler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _uretimUrunlerRepo.GetAll2(filter)


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

						 select new UretimUrunlerVM
						 {
							 ID = kayit.ID, 
							 UrunAd = urunler.Ad != null ? urunler.Ad : "",
							 UrunKod = urunler.Kod != null ? urunler.Kod : "",
							 UrunID = kayit.UrunID,
							  Deger=kayit.Deger,
							  UretimID=kayit.UretimID,
							 BirimID = urunler.BirimID,
							 BirimAd = birim.Ad,
							 TipAd = tip.Ad,
							 TipID = tip.ID,
							  
							 DovizTur = urunler.DovizTur,
							 DovizTurAd = doviz.Ad,

							 Grup = grup.Ad,
							 AltGrup = altGrup.Ad,
							 GrupID = grup.ID,
							 AltGrupID = altGrup.ID,
							  


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
		public async Task<List<UretimUrunlerVM>> UretimUrunlerGetir(int UretimID)
		{
			if (UretimID != 0)
			{
				try
				{
					var list = await UretimUrunlerGenelListe().Where(p => p.UretimID == UretimID).ToListAsync();

					return list;
				}
				catch (Exception ex)
				{

					return new List<UretimUrunlerVM>();
				}



			}
			else
			{
				return new List<UretimUrunlerVM>();
			}
		}

		public Task<UretimVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(UretimVM model)
		{
			throw new NotImplementedException();
		}
		public async Task<bool> VeriEkleAsync(UretimVM model)
		{

			var parametreler = model.UretimParametreDeger;
			if (parametreler != null && parametreler.Any())
			{
				foreach (var item in parametreler)
				{
					var mevcut = await _uretimParametreDegerRepo.GetByIdAsync(item.ID);
					if (mevcut != null)
					{
						mevcut.Deger = item.Deger;
						mevcut.UpdateDate = DateTime.UtcNow;
						mevcut.UpdateUserID = _userId;

						await _uretimParametreDegerRepo.UpdateAsync(mevcut);
					}
				}
			}
			var urunler = model.UretimUrunler;
			if (urunler != null && urunler.Any())
			{
				foreach (var item in urunler)
				{
					var mevcut = await _uretimUrunlerRepo.GetByIdAsync(item.ID);
					if (mevcut != null)
					{
						mevcut.Deger = item.Deger;
						mevcut.UpdateDate = DateTime.UtcNow;
						mevcut.UpdateUserID = _userId;

						await _uretimUrunlerRepo.UpdateAsync(mevcut);
					}
				}
			}

			//var carpan=para




			model.Not = model.Not.Replace("\r\n", "");
			Uretim? existingEntry = _uretimRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Uretim>(model);
				_uretimRepo.Add(newEntry);
			}
			else
			{
				 model.KayipFireMiktar = model.HesaplananMiktar-model.GerceklesenMiktar;
				model.KayipFireOran = (double)((model.HesaplananMiktar - model.GerceklesenMiktar) / model.HesaplananMiktar * 100);
				model.ParasalDeger = (model.HesaplananMiktar - model.GerceklesenMiktar) * model.HmCarpan;
				_mapper.Map(model, existingEntry);
				await _uretimRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<UretimVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new UretimVM();
			}

			UretimVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new UretimVM();
			}

			return kayit;
		}

		public async Task<List<UretimVM>> VeriListele(UretimVM model)
		{

			//var malzemeList = JsonSerializer.Deserialize<List<TagifyMalzemeDto>>(model.tagifyMalzemeler);

			//var idList = malzemeList.Select(x => int.Parse(x.Id)).ToList();
			List<TagifyDto> malzemeList = null;

			// Tagify alanı doluysa parse et
			if (!string.IsNullOrWhiteSpace(model.tagifyMalzemeler))
			{
				malzemeList = JsonSerializer.Deserialize<List<TagifyDto>>(model.tagifyMalzemeler);
			}

			// ID listesi doluysa dolu olsun, değilse boş liste olsun
			var idList = (malzemeList != null)
				? malzemeList.Select(x => int.Parse(x.Id)).ToList()
				: new List<int>();



			var liste = GenelListe();

			if (idList.Any())
			{
				liste = liste.Where(x => idList.Contains(x.UrunID));
			}

			if (!string.IsNullOrWhiteSpace(model.Aciklama))
			{
				liste = liste.Where(p => p.Not.Contains(model.Aciklama));
			}

			if (model.SiparisTarihiBas != null || model.SiparisTarihiSon != null)
			{
				liste = liste.Where(p => p.SiparisTarihi >= model.SiparisTarihiBas && p.SiparisTarihi <= model.SiparisTarihiSon);
			}
			//if (model.Aciklama != null)
			//{
			//	liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama));
			//}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<UretimVM>> VeriListele()
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

		public async Task<PagedResult<UretimVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
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

				return new PagedResult<UretimVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<UretimVM>
				{
					Items = new List<UretimVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public Task<PagedResult<UretimVM>> VeriListeleAsync(UretimVM model)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> VeriSil(int id)
		{
			Uretim? kayit = _uretimRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _uretimRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}
	}
}
