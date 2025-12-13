 
using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading.Tasks;


namespace Ekomers.Data.Services
{
	public class EczaneService : IEczaneService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory ;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Eczane> _EczaneRepo;
		private readonly IRepository<Geojson> _mapRespository;
		private readonly ICacheService<Sehirler> _sehirlerCache;
		private readonly IFileService _fileService;
		private readonly ISehirlerService _sehirlerService;
		private readonly IRepository<Sehirler> _sehirRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public EczaneService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Eczane> EczaneRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo
			, IHttpClientFactory httpClientFactory
			, IFileService fileService
			,ISehirlerService sehirlerService
			, ICacheService<Sehirler> sehirlerCache
			, IRepository<Sehirler> sehirRepo
			, IRepository<Geojson> mapRespository
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_EczaneRepo = EczaneRepo;
			_userRepo = userRepo;
			_sehirlerService = sehirlerService;
			_sehirlerCache = sehirlerCache;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
			_fileService = fileService;
			_sehirRepo = sehirRepo;
			_mapRespository = mapRespository;
		}

		public async Task<bool> EczaneAktar(List<Eczane> liste)
		{
			if (liste == null || liste.Count == 0)
				return false;

			// 1) Gelen veriyi normalize edip anahtar üret
			var incoming = liste.Select(x => new
			{
				Key = $"{(x.Sehir ?? string.Empty).Trim().ToUpper()}|" +
					  $"{(x.Ad ?? string.Empty).Trim().ToUpper()}|" +
					  $"{(x.Telefon ?? string.Empty).Trim()}",
				Item = x
			}).ToList();

			// Aynı eczane birden fazla kez geldiyse gereksiz sorguyu azalt
			var keys = incoming.Select(i => i.Key).Distinct().ToList();

			// 2) DB'de bu anahtarlardan hangileri var, tek sorguda çek
			var existingKeys = await _context.Eczane
				.AsNoTracking()
				.Where(e =>
					keys.Contains(
						((e.Sehir ?? "")).Trim().ToUpper() + "|" +
						((e.Ad ?? "")).Trim().ToUpper() + "|" +
						((e.Telefon ?? "")).Trim()
					))
				.Select(e =>
					((e.Sehir ?? "")).Trim().ToUpper() + "|" +
					((e.Ad ?? "")).Trim().ToUpper() + "|" +
					((e.Telefon ?? "")).Trim()
				)
				.ToListAsync();

			var existingSet = new HashSet<string>(existingKeys);

			// 3) Sadece yeni olanları ekle
			var newOnes = incoming
				.Where(i => !existingSet.Contains(i.Key))
				.Select(i => i.Item)
				.ToList();

			if (newOnes.Count == 0)
				return false;

			_EczaneRepo.AddRange(newOnes);
			await _context.SaveChangesAsync();
			return true;
		}

		//public async Task<bool> EczaneAktar(List<Eczane> liste)
		//{
		//	if (liste == null || liste.Count == 0)
		//		return false;

		//	foreach (var model in liste)
		//	{
		//		var existingEntry = await _context.Eczane.Where(p => p.Sehir == model.Sehir && p.Ad == model.Ad && p.Telefon == model.Telefon).FirstOrDefaultAsync();
		//		if (existingEntry == null)
		//		{
		//			_EczaneRepo.Add(model);
		//		}

		//	}

		//	// Değişiklikleri veritabanına yansıt
		//	_context.SaveChanges();

		//	return true;

		//}

		public IQueryable<EczaneVM> GenelListe()
		{
			Expression<Func<Eczane, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _EczaneRepo.GetAll2(filter)

						 join geojson in _mapRespository.GetAll2(p=>p.ModulID==(int)ModulEnum.Eczane) on kayit.ID equals geojson.KayitID
					   into geojsonGroup
						 from geojson in geojsonGroup.DefaultIfEmpty()
						 

						 join sehir in _sehirRepo.GetAll2() on kayit.SehirID equals sehir.ID
					   into sehirGroup
						 from sehir in sehirGroup.DefaultIfEmpty()

						 join ilce in _sehirRepo.GetAll2() on kayit.IlceID equals ilce.ID
						 into ilceGroup
						 from ilce in ilceGroup.DefaultIfEmpty()

						 join mahalle in _sehirRepo.GetAll2() on kayit.MahalleID equals mahalle.ID
						into mahalleGroup
						 from mahalle in mahalleGroup.DefaultIfEmpty()

						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new EczaneVM
						 {
							 ID = kayit.ID,
							 Ad = kayit.Ad,
							 Aciklama = kayit.Aciklama,
							 Telefon = kayit.Telefon,
							 Adres = kayit.Adres,
							 Il = kayit.Il,
							// Ilce = kayit.Ilce,
							 AciklamaAdres = kayit.AciklamaAdres,
							 Boylam = kayit.Boylam,
							 Enlem = kayit.Enlem,
							 Konum= kayit.Konum,
							 Tarih= kayit.Tarih ,
							 TarihDetay= kayit.TarihDetay,
								Plaka = kayit.Plaka, 
								BayiMi= kayit.BayiMi,
								Email1= kayit.Email1,
								Email2= kayit.Email2,
								Telefon2 = kayit.Telefon2,
								MusteriAdi=kayit.MusteriAdi,
								EczaciAdi=kayit.EczaciAdi,
								Fotograf=kayit.Fotograf,
							 Sehir = sehir != null ? sehir.Ad : "",
							 SehirID = kayit.SehirID == null ? 0 : kayit.SehirID,
							 Ilce = ilce != null ? ilce.Ad : "",
							 IlceID = kayit.IlceID == null ? 0 : kayit.IlceID,
							 Mahalle = mahalle != null ? mahalle.Ad : "",
							 MahalleID = kayit.MahalleID,
							 IsMap= geojson !=null ? true : false,
							 Veri=geojson.Veri,


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



		public Task<EczaneVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(EczaneVM model)
		{
			if(model.Aciklama != null)
			{
				model.Aciklama = model.Aciklama.Replace("\r\n", "");
			}
			 
			Eczane? existingEntry = _EczaneRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Eczane>(model);
				_EczaneRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_EczaneRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<EczaneVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new EczaneVM();
			}

			EczaneVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new EczaneVM();
			}

			return kayit;
		}

		public async Task<List<EczaneVM>> VeriListele(EczaneVM model)
		{
			var liste = GenelListe();

			if (model.SehirID != 0)
			{
				liste = liste.Where(p => Convert.ToInt32(p.Plaka) == model.SehirID);
			}

			if (model.BayiMi != null)
			{
				liste = liste.Where(p => p.BayiMi==model.BayiMi);
			}

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.Ad.Contains(model.Aciklama) ||
				p.Adres.Contains(model.Aciklama) ||
				p.AciklamaAdres.Contains(model.Aciklama) ||
				p.Telefon.Contains(model.Aciklama) ||
				p.Ilce.Contains(model.Aciklama) ||
				p.Il.Contains(model.Aciklama) ||
				p.Sehir.Contains(model.Aciklama) ||
				p.Konum.Contains(model.Aciklama) 
				);
			}

			var donus =await  liste.OrderByDescending(a => a.ID).Take(1000).ToListAsync();
			return donus;
		}

		public async Task<List<EczaneVM>> VeriListele()
		{
			//.OrderByDescending(a => a.ID)
			try
			{
				var List = await GenelListe()
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<EczaneVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			Eczane? kayit = _EczaneRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_EczaneRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<bool> OmtWebeGonder(string sehirlerCacheKeys)
		{
			 
			//.Select(p =>
			//new EczaneVM
			//{
			//	Ad = p.Ad,
			//	Adres = p.Adres,
			//	ID = p.ID,
			//	Telefon = p.Telefon,
			//	Sehir = p.Sehir,
			//	SehirID = p.SehirID,
			//	Ilce = p.Ilce,
			//	Il = p.Il,
			//	Aciklama = p.Aciklama,
			//	AciklamaAdres = p.AciklamaAdres,
			//	Email1 = p.Email1,
			//	Email2 = p.Email2,
			//	Veri = p.Veri,
			//	EczaciAdi = p.EczaciAdi,
			//	IlceID = p.IlceID,
			//	Mahalle = p.Mahalle,
			//	MahalleID = p.MahalleID,
			//	MusteriAdi = p.MusteriAdi,

			//})


			var dataEczane = GenelListe().Where(p=>p.BayiMi==false).ToList(); // Veriyi IQueryable'dan bir listeye dönüştürüyoruz.
			Expression<Func<Sehirler, bool>> filter = a => a.UstID < 82;
			var dataSehirler = await _context.Sehirler.Where(filter).ToListAsync();

			var jsonOptions = new JsonSerializerOptions
			{
				WriteIndented = true, // JSON dosyasının daha okunabilir olmasını sağlar.
				Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // Türkçe karakterlerin doğru kodlanmasını sağlar.
			};

			string jsonDataEczane = JsonSerializer.Serialize(dataEczane, jsonOptions); // Veriyi JSON stringe çeviriyoruz.
			string jsonDataSehirler = JsonSerializer.Serialize(dataSehirler, jsonOptions);

			string ftpUrl = "ftp://ortomolekuler.com/eczane.ortomolekuler.com/wwwroot/Data";
			var ftpUserName = "ortomolekuler";
			var ftpUserPassword = "H@UznpocvVz48t2^";

			try
			{
				bool result1 = await _fileService.UploadDataToFTP(jsonDataEczane, "Eczaneler.json", ftpUrl, ftpUserName, ftpUserPassword);
				bool result2 = await _fileService.UploadDataToFTP(jsonDataSehirler, "Sehirler.json", ftpUrl, ftpUserName, ftpUserPassword);

				if (!result2 && !result1)
				{
					return false;
				}

				//if (!result1)
				//{
				//	return false;
				//}
			}
			catch (Exception ex)
			{

				return false;
			}
			



			 



			return true;
		 
		}
		public async Task<bool> FwWebeGonder(string sehirlerCacheKeys)
		{

		 


			var dataEczane = GenelListe().ToList(); // Veriyi IQueryable'dan bir listeye dönüştürüyoruz.
			Expression<Func<Sehirler, bool>> filter = a => a.UstID < 82;
			var dataSehirler = await _context.Sehirler.Where(filter).ToListAsync();

			var jsonOptions = new JsonSerializerOptions
			{
				WriteIndented = true, // JSON dosyasının daha okunabilir olmasını sağlar.
				Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // Türkçe karakterlerin doğru kodlanmasını sağlar.
			};

			string jsonDataEczane = JsonSerializer.Serialize(dataEczane, jsonOptions); // Veriyi JSON stringe çeviriyoruz.
			string jsonDataSehirler = JsonSerializer.Serialize(dataSehirler, jsonOptions);

			string ftpUrl = "ftp://ortomolekulertipdernegi.com/eczane.fwilac.com.tr/wwwroot/Data";
			var ftpUserName = "ortomolekulertip";
			var ftpUserPassword = "3w5XgMxcn3v_nH$y";

			try
			{
				bool result1 = await _fileService.UploadDataToFTP(jsonDataEczane, "Eczaneler.json", ftpUrl, ftpUserName, ftpUserPassword);
				bool result2 = await _fileService.UploadDataToFTP(jsonDataSehirler, "Sehirler.json", ftpUrl, ftpUserName, ftpUserPassword);

				if (!result2 && !result1)
				{
					return false;
				}

				//if (!result1)
				//{
				//	return false;
				//}
			}
			catch (Exception ex)
			{

				return false;
			}








			return true;

		}

		 
		public void FotoYukle(EczaneVM model)
		{
			var eczane = _EczaneRepo.GetById(model.ID);
			if (eczane != null)
			{
				eczane.Fotograf = model.Fotograf;
				_EczaneRepo.Update(eczane);
				_context.SaveChanges();
			}

		}
	}
}
