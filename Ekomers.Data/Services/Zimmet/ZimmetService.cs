using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services
{
	public class ZimmetService : IZimmetService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<Envanter> _EnvanterRepo;
		private readonly IRepository<Zimmet> _ZimmetRepo;
		private readonly IRepository<Personel> _PersonelRepo;
		private readonly IRepository<Sirketler> _SirketlerRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IFileService _fileService;

		public ZimmetService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Zimmet> ZimmetRepo, IHttpClientFactory httpClientFactory, 
			IFileService fileService, IRepository<Kullanici> userRepo, IRepository<Envanter> EnvanterRepo
			, IRepository<Sirketler> SirketlerRepo, IRepository<Personel> PersonelRepo
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper; 
			_EnvanterRepo = EnvanterRepo; 
			_userRepo = userRepo;
			_ZimmetRepo = ZimmetRepo;
			_SirketlerRepo = SirketlerRepo;
			_PersonelRepo = PersonelRepo;
			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_httpClientFactory = httpClientFactory;
			_fileService = fileService;
		}

		public IQueryable<ZimmetVM> GenelListe()
		{
			
			var result = from kayit in _ZimmetRepo.GetAll2()

						 join e in _EnvanterRepo.GetAll2() on kayit.EnvanterID equals e.ID
						  into envanterGroup
						 from e in envanterGroup.DefaultIfEmpty()

						 join p in _PersonelRepo.GetAll2() on kayit.PersonelID equals p.ID
						 into personelGroup
						 from p in personelGroup.DefaultIfEmpty()

						 join s in _SirketlerRepo.GetAll2() on e.SirketID equals s.ID
						 into sirketGroup
						 from s in sirketGroup.DefaultIfEmpty()



						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()



						 select new ZimmetVM
						 {
							 ID = kayit.ID,
							 EnvanterID = kayit.EnvanterID,
							 Envanter = _mapper.Map<EnvanterVM>(e)??new EnvanterVM(),
							 Sirket = s??new Sirketler(),
							 PersonelID = kayit.PersonelID,
							 Personel = p??new Personel(),
							 ZimmetTarihi = kayit.ZimmetTarihi,
							 TeslimTarihi = kayit.TeslimTarihi,
							 AciklamaIlk = kayit.AciklamaIlk,
							 AciklamaSon = kayit.AciklamaSon,
							 DurumID = kayit.DurumID,
							 

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


			return result ;
		}

		public Task<ZimmetVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(ZimmetVM model)
		{

			Zimmet? existingEntry = _ZimmetRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Zimmet>(model);
				_ZimmetRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_ZimmetRepo.Update(existingEntry);
			}

			_context.SaveChanges(); 
			return true;
		}
		public async Task<bool> VeriEkleAsync(ZimmetVM model)
		{

			Zimmet? existingEntry = _ZimmetRepo.GetById(model.ID);
			if (existingEntry == null)
			{
				var newEntry = _mapper.Map<Zimmet>(model);
				_ZimmetRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				await _ZimmetRepo.UpdateAsync(existingEntry);
			}

			await _context.SaveChangesAsync();
		 
			return true;
		}

		public async Task<ZimmetVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new ZimmetVM();
			}

			ZimmetVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new ZimmetVM();
			}

			return kayit;
		}
		public async Task<List<ZimmetVM>> VeriListele(ZimmetVM model)
		{



			var liste = GenelListe();


			//if (model.Aciklama != null)
			if (!string.IsNullOrWhiteSpace(model.AciklamaIlk))
			{
				liste = liste.Where(p => p.AciklamaIlk.Contains(model.AciklamaIlk)  
				);
			}
			//if (model.GrupID != 0)
			//{
			//	liste = liste.Where(p =>p.AltGrupID==model.GrupID);
			//}


			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}
		public async Task<PagedResult<ZimmetVM>> VeriListeleAsync(ZimmetVM modelv)
		{
			try
			{

				if (modelv.PageIndex < 1) modelv.PageIndex = 1;
				// mantıklı bir üst sınır koy
				if (modelv.PageSize <= 0 || modelv.PageSize > 1000) modelv.PageSize = 10;

				var query = GenelListe();



				if (modelv.AciklamaIlk != null)
				{
					query = query.Where(p => p.AciklamaIlk.Contains(modelv.AciklamaIlk)  
					);
				}

				var total = await query.CountAsync(modelv.ct);

				var items = await query
					.OrderByDescending(a => a.ID)
					.Skip((modelv.PageIndex - 1) * modelv.PageSize)
					.Take(modelv.PageSize)
					.ToListAsync(modelv.ct);

				return new PagedResult<ZimmetVM>
				{
					Items = items,
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<ZimmetVM>
				{
					Items = new List<ZimmetVM>(),
					PageIndex = modelv.PageIndex,
					PageSize = modelv.PageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<List<ZimmetVM>> VeriListele()
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
			Zimmet? kayit = _ZimmetRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				await _ZimmetRepo.UpdateAsync(kayit);
				await _context.SaveChangesAsync();
				 
			}
			return true;
		}

		public async Task<PagedResult<ZimmetVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			try
			{
				if (page < 1) page = 1;
				// mantıklı bir üst sınır koy
				if (pageSize <= 0 || pageSize > 1000) pageSize = 50;

				var query = GenelListe(); // IQueryable<ZimmetVM>

				var total = await query.CountAsync(ct);

				var items = await query
					.OrderBy(a => a.ID)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync(ct);

				return new PagedResult<ZimmetVM>
				{
					Items = items,
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = total
				};
			}
			catch
			{
				return new PagedResult<ZimmetVM>
				{
					Items = new List<ZimmetVM>(),
					PageIndex = page,
					PageSize = pageSize,
					TotalCount = 0
				};
			}
		}

		public async Task<ZimmetVM> ZimmetGetir(int envanterID)
		{
			if (envanterID <= 0)
			{
				return new ZimmetVM();
			}

			ZimmetVM kayit = GenelListe().Where(p => p.EnvanterID == envanterID).FirstOrDefault();

			if (kayit == null)
			{
				return new ZimmetVM();
			}

			return kayit;
		}
	}
}
