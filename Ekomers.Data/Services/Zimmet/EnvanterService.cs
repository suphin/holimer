 

 
using AutoMapper;
using Azure;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
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
	public class EnvanterService : IEnvanterService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Envanter> _EnvanterRepo;
		private readonly IRepository<Zimmet> _ZimmetRepo;
		private readonly IRepository<Personel> _PersonelRepo;
		private readonly IRepository<EnvanterDepartman> _EnvanterDepartmanRepo;
		private readonly IRepository<EnvanterBolum> _EnvanterBolumRepo;

		private readonly IRepository<Sirketler> _SirketlerRepo;
		 
		private readonly IRepository<Geojson> _mapRespository;
		private readonly IRepository<EnvanterTur> _envanterTurRepo;
		private readonly IRepository<EnvanterTip> _envanterTipRepo;
		private readonly IFileService _fileService;
		 
		 
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public EnvanterService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo 
			,IRepository<Envanter> EnvanterRepo
			, IRepository<EnvanterTur> envanterTurRepo
			, IRepository<EnvanterTip> envanterTipRepo
			, IRepository<EnvanterDepartman> EnvanterDepartmanRepo
			, IRepository<EnvanterBolum> EnvanterBolumRepo
			, IRepository<Sirketler> SirketlerRepo
			, IHttpClientFactory httpClientFactory
			, IFileService fileService 
			, IRepository<Zimmet> ZimmetRepo
			, IRepository<Personel> PersonelRepo
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;
			_SirketlerRepo = SirketlerRepo;
			_EnvanterRepo = EnvanterRepo;
			_envanterTurRepo = envanterTurRepo;
			_EnvanterDepartmanRepo = EnvanterDepartmanRepo;
			_EnvanterBolumRepo = EnvanterBolumRepo;
			_ZimmetRepo = ZimmetRepo;
			_userRepo = userRepo;
			_envanterTipRepo = envanterTipRepo;

			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
			_fileService = fileService;
			_PersonelRepo = PersonelRepo;
		}

	 

		public IQueryable<EnvanterVM> GenelListe()
		{
			Expression<Func<Envanter, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}

			var result = from kayit in _EnvanterRepo.GetAll2(filter)

						 join zimmet in _ZimmetRepo.GetAll2() on kayit.ID equals zimmet.EnvanterID
						 into zimmetGroup
						 from zimmet in zimmetGroup.DefaultIfEmpty()
						  
						 join departman in _EnvanterDepartmanRepo.GetAll2() on kayit.EnvanterDepartmanID equals departman.ID
						 into departmanGroup
						 from departman in departmanGroup.DefaultIfEmpty()

						 join bolum in _EnvanterBolumRepo.GetAll2() on kayit.EnvanterBolumID equals bolum.ID
						 into bolumGroup
						 from bolum in bolumGroup.DefaultIfEmpty()

						 join demirbasTip in _envanterTipRepo.GetAll2() on kayit.TipID equals demirbasTip.ID
						 into demirbasTipGroup
						 from demirbasTip in demirbasTipGroup.DefaultIfEmpty()

						 join demirbastur in _envanterTurRepo.GetAll2() on kayit.TurID equals demirbastur.ID
						 into demirbasturGroup
						 from demirbastur in demirbasturGroup.DefaultIfEmpty()

						 join sirket in _SirketlerRepo.GetAll2() on kayit.SirketID equals sirket.ID
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

						 select new EnvanterVM
						 {
							 ID = kayit.ID,
							 Model = kayit.Model,
							 Marka = kayit.Marka,
							 SeriNo = kayit.SeriNo,
							 YerKodu = departman.Kod + "_" + bolum.Kod + "_" + demirbastur.Kod + "_" + kayit.Numara,
							 AlimTarihi = kayit.AlimTarihi,
							 GarantiBas = kayit.GarantiBas,
							 GarantiBit = kayit.GarantiBit,
							 Aciklama = kayit.Aciklama,
							 AlimFiyati = kayit.AlimFiyati,
							 DovizKuru = kayit.DovizKuru,
							 DovizTuru = kayit.DovizTuru,
							 Numara= kayit.Numara,
							 Ad=kayit.Ad,
							 EnvanterBolumID = kayit.EnvanterBolumID,
							 Bolum = bolum != null ? bolum.Ad : "",
							 BolumKod = bolum != null ? bolum.Kod : "",
							 Fotograf=kayit.Fotograf,
							 EnvanterDepartmanID = kayit.EnvanterDepartmanID,
							 Departman = departman != null ? departman.Ad : "",
							 DepartmanKod = departman != null ? departman.Kod : "",

							 TurID = kayit.TurID,
							 Tur = demirbastur != null ? demirbastur.Ad : "",
							 TurKod = demirbastur != null ? demirbastur.Kod : "",

							 TipID = kayit.TipID,
							 Tip = demirbasTip != null ? demirbasTip.Ad : "",
							 OdaID = kayit.OdaID, 

							 SirketID = kayit.SirketID,
							 Sirket = sirket != null ? sirket.SirketAdi : "",

							 Zimmet = zimmet ?? new Zimmet(),

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

		public IQueryable<EnvanterVM> GenelListeZimmet(int personelID)
		{

			var result = from kayit in _EnvanterRepo.GetAll2()

						 join zimmet in _ZimmetRepo.GetAll2(z => z.PersonelID == personelID) on kayit.ID equals zimmet.EnvanterID							 
						 into zimmetGroup 						 
						 from zimmet in zimmetGroup.DefaultIfEmpty()

						 
							 //join personel in _PersonelRepo.GetAll2() on zimmet.PersonelID equals personel.ID
							 //into personelGroup
							 //from personel in personelGroup.DefaultIfEmpty()


						 join departman in _EnvanterDepartmanRepo.GetAll2() on kayit.EnvanterDepartmanID equals departman.ID
						 into departmanGroup
						 from departman in departmanGroup.DefaultIfEmpty()

						 join bolum in _EnvanterBolumRepo.GetAll2() on kayit.EnvanterBolumID equals bolum.ID
						 into bolumGroup
						 from bolum in bolumGroup.DefaultIfEmpty()

						 join demirbasTip in _envanterTipRepo.GetAll2() on kayit.TipID equals demirbasTip.ID
						into demirbasTipGroup
						 from demirbasTip in demirbasTipGroup.DefaultIfEmpty()

						 join demirbastur in _envanterTurRepo.GetAll2() on kayit.TurID equals demirbastur.ID
						 into demirbasturGroup
						 from demirbastur in demirbasturGroup.DefaultIfEmpty()

						 join sirket in _SirketlerRepo.GetAll2() on kayit.SirketID equals sirket.ID
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

						 select new EnvanterVM
						 {
							 ID = kayit.ID,
							 Model = kayit.Model,
							 Marka = kayit.Marka,
							 SeriNo = kayit.SeriNo,
							 YerKodu = departman.Kod + "_" + bolum.Kod + "_" + demirbastur.Kod + "_" + kayit.Numara,
							 AlimTarihi = kayit.AlimTarihi,
							 GarantiBas = kayit.GarantiBas,
							 GarantiBit = kayit.GarantiBit,
							 Aciklama = kayit.Aciklama,
							 AlimFiyati = kayit.AlimFiyati,
							 DovizKuru = kayit.DovizKuru,
							 DovizTuru = kayit.DovizTuru,
							 Numara = kayit.Numara,
							 Ad = kayit.Ad,
							 EnvanterBolumID = kayit.EnvanterBolumID,
							 Bolum = bolum != null ? bolum.Ad : "",
							 BolumKod = bolum != null ? bolum.Kod : "",
							 Fotograf = kayit.Fotograf,
							 EnvanterDepartmanID = kayit.EnvanterDepartmanID,
							 Departman = departman != null ? departman.Ad : "",
							 DepartmanKod = departman != null ? departman.Kod : "",

							 TurID = kayit.TurID,
							 Tur = demirbastur != null ? demirbastur.Ad : "",
							 TurKod = demirbastur != null ? demirbastur.Kod : "",

							 TipID = kayit.TipID,
							 Tip = demirbasTip != null ? demirbasTip.Ad : "",
							 OdaID = kayit.OdaID,
							 

							 SirketID = kayit.SirketID,
							 Sirket = sirket != null ? sirket.SirketAdi : "",
							 //Zimmet= zimmet,
							 PersonelID=zimmet.PersonelID !=null ? zimmet.PersonelID:0,

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

		public Task<EnvanterVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}
		public async Task<int> VeriEkleReturnIDAsync(EnvanterVM model)
		{
			model.Aciklama = (model.Aciklama ?? string.Empty).Replace("\r\n", "");

			Envanter entity;

			// Güncelleme mi, ekleme mi?
			if (model.ID > 0)
			{
				// Tercihen async repo kullan
				entity = await _EnvanterRepo.GetByIdAsync(model.ID);
				if (entity == null)
					throw new KeyNotFoundException($"Envanter {model.ID} bulunamadı.");

				_mapper.Map(model, entity);
				await _EnvanterRepo.UpdateAsync(entity);
			}
			else
			{
				entity = _mapper.Map<Envanter>(model);

				// Tercihen async ekleme
				await _EnvanterRepo.AddAsync(entity);
				// Eğer Add async değilse: _TeklifRepo.Add(entity);
			}

			await _context.SaveChangesAsync();

			// EF Core SaveChanges sonrası ID atanır
			return entity.ID;
		}

		public bool VeriEkle(EnvanterVM model)
		{
			if (model.Aciklama != null)
			{
				model.Aciklama = model.Aciklama.Replace("\r\n", "");
			}
			if (model.Aciklama==null)
			{
				model.Aciklama = string.Empty;
			}

			Envanter? existingEntry = _EnvanterRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Envanter>(model);
				_EnvanterRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_EnvanterRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}
		public async Task<bool> VeriEkleAsync(EnvanterVM model)
		{

			Envanter? existingEntry = _EnvanterRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Envanter>(model);
				_EnvanterRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _EnvanterRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
			 
			return true;
		}

		public async Task<EnvanterVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new EnvanterVM();
			}

			EnvanterVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new EnvanterVM();
			}

			return kayit;
		}

		public async Task<List<EnvanterVM>> VeriListele(EnvanterVM model)
		{
			var liste = GenelListe();

			if (model.TurID != 0)
			{
				liste = liste.Where(p => p.TurID == model.TurID);
			}
			if (model.TipID != 0)
			{
				liste = liste.Where(p => p.TipID == model.TipID);
			}
			if (model.EnvanterBolumID != 0)
			{
				liste = liste.Where(p => p.EnvanterBolumID == model.EnvanterBolumID);
			}
			if (model.EnvanterDepartmanID != 0)
			{
				liste = liste.Where(p => p.EnvanterDepartmanID == model.EnvanterDepartmanID);
			}
			if (model.SirketID != 0)
			{
				liste = liste.Where(p => p.SirketID == model.SirketID);
			}

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.Marka.Contains(model.Aciklama) ||
				p.Model.Contains(model.Aciklama) ||
				p.Ad.Contains(model.Aciklama) ||
				p.Departman.Contains(model.Aciklama) ||
				p.Bolum.Contains(model.Aciklama) ||
				p.SeriNo.Contains(model.Aciklama)
				);
			}

			var donus = await liste.OrderByDescending(a => a.ID).Take(1000).ToListAsync();
			return donus;
		}

		public async Task<List<EnvanterVM>> VeriListele()
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

				return new List<EnvanterVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			Envanter? kayit = _EnvanterRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_EnvanterRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		 

		public void FotoYukle(EnvanterVM model)
		{
			var Envanter = _EnvanterRepo.GetById(model.ID);
			if (Envanter != null)
			{
				Envanter.Fotograf = model.Fotograf;
				_EnvanterRepo.Update(Envanter);
				_context.SaveChanges();
			}

		}

		public Task<List<EnvanterBolum>> GetBolumler(int ParametreID)
		{
			var bolumler = _EnvanterBolumRepo.GetAll().Where(p => p.EnvanterDepartmanID == ParametreID).ToList();
			return Task.FromResult(bolumler);

		}

		public async Task<PagedResult<EnvanterVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var liste = GenelListe();  







				var total = await liste.CountAsync(ct);

				var items = await liste
					.OrderByDescending(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<EnvanterVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<EnvanterVM>
				{
					Items = new List<EnvanterVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<PagedResult<EnvanterVM>> VeriListeleAsync(EnvanterVM model)
		{
			try
			{
				if (model.PageIndex < 1) model.PageIndex = 1;
				// mantıklı bir üst sınır koy
				if (model.PageSize <= 0 || model.PageSize > 1000) model.PageSize = 50;

				var liste = GenelListe();


				if (model.TurID != 0)
				{
					liste = liste.Where(p => p.TurID == model.TurID);
				}
				if (model.EnvanterBolumID != 0)
				{
					liste = liste.Where(p => p.EnvanterBolumID == model.EnvanterBolumID);
				}
				if (model.EnvanterDepartmanID != 0)
				{
					liste = liste.Where(p => p.EnvanterDepartmanID == model.EnvanterDepartmanID);
				}
				if (model.SirketID != 0)
				{
					liste = liste.Where(p => p.SirketID == model.SirketID);
				}

				if (model.Aciklama != null)
				{
					liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
					p.Marka.Contains(model.Aciklama) ||
					p.Model.Contains(model.Aciklama) ||
					p.Ad.Contains(model.Aciklama) ||
					p.Departman.Contains(model.Aciklama) ||
					p.Bolum.Contains(model.Aciklama) ||
					p.SeriNo.Contains(model.Aciklama)
					);
				}





				var total = await liste.CountAsync(model.ct);

				var items = await liste
					.OrderByDescending(a => a.ID)
					.Skip((model.PageIndex - 1) * model.PageSize)
					.Take(model.PageSize)
					.ToListAsync(model.ct);

				return new PagedResult<EnvanterVM>
				{
					Items = items,
					PageIndex = model.PageIndex,
					PageSize = model.PageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<EnvanterVM>
				{
					Items = new List<EnvanterVM>(),
					PageIndex = model.PageIndex,
					PageSize = model.PageSize,
					TotalCount = 0
				};
			}
		}

		public async  Task<List<EnvanterVM>> VeriListeleZimmet(int personelID)
		{
			var list = GenelListeZimmet(personelID).ToList();
			return list;
		}
	}
}
