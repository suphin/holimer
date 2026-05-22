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
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		private readonly IFileService _fileService;

		public ZimmetService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Zimmet> ZimmetRepo, IHttpClientFactory httpClientFactory, 
			IFileService fileService, IRepository<Kullanici> userRepo, IRepository<Envanter> EnvanterRepo
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper; 
			_EnvanterRepo = EnvanterRepo; 
			_userRepo = userRepo;
			_ZimmetRepo = ZimmetRepo;


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

						 join k in _userRepo.GetAll2() on kayit.KullaniciID equals k.Id
						 into kullaniciGroup
						 from k in kullaniciGroup.DefaultIfEmpty()




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
							 Envanter = _mapper.Map<EnvanterVM>(e),
							 KullaniciID = kayit.KullaniciID,
							 Kullanici = k,
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
			throw new NotImplementedException();
		}

		public Task<ZimmetVM> VeriGetir(int id)
		{
			throw new NotImplementedException();
		}

		public Task<List<ZimmetVM>> VeriListele(ZimmetVM model)
		{
			throw new NotImplementedException();
		}

		public Task<List<ZimmetVM>> VeriListele()
		{
			throw new NotImplementedException();
		}

		public Task<PagedResult<ZimmetVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default)
		{
			throw new NotImplementedException();
		}

		public Task<PagedResult<ZimmetVM>> VeriListeleAsync(ZimmetVM model)
		{
			throw new NotImplementedException();
		}

		public Task<bool> VeriSil(int id)
		{
			throw new NotImplementedException();
		}
	}
}
