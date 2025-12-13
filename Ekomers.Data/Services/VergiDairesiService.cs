using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class VergiDairesiService : IVergiDairesiService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;
		private readonly IRepository<Dosya> _dosyaRepo;
		private readonly IRepository<Kullanici> _userRepo;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;

		private readonly IRepository<VergiDairesi> _VergiDairesiRepo;


		public VergiDairesiService(ApplicationDbContext context,
				 IHttpContextAccessor httpContextAccessor,
				 IMapper mapper,
				 IRepository<Kullanici> userRepo,
				 IRepository<Dosya> dosyaRepo, IRepository<VergiDairesi> VergiDairesiRepo)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;
			_dosyaRepo = dosyaRepo;
			_userRepo = userRepo;

			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			_VergiDairesiRepo = VergiDairesiRepo;
		}
		public IQueryable<VergiDairesiVM> GenelListe()
		{
			Expression<Func<VergiDairesi, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _VergiDairesiRepo.GetAll2(filter)


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						  into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()




						 select new VergiDairesiVM
						 {
							 ID = kayit.ID,
							 Ad = kayit.Ad,
							 Kod = kayit.Kod,
							 Ilce = kayit.Ilce,
							 SehirID = kayit.SehirID,
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

		public async Task<List<VergiDairesiVM>> GetVergiDairesi(int ParametreID)
		{
			try
			{
				var List = await GenelListe().Where(p => p.SehirID == ParametreID).OrderBy(a => a.ID)
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<VergiDairesiVM>();
			}
		}

		public Task<VergiDairesiVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(VergiDairesiVM model)
		{
			VergiDairesi? kayit = _VergiDairesiRepo.GetById(model.ID);
			if (kayit == null)
			{

				var newEntry = _mapper.Map<VergiDairesi>(model);
				//newEntry.CreateDate = DateTime.Now;
				//newEntry.IsActive = true;
				//newEntry.IsDelete = false;
				//newEntry.CreateUserID = _userId;

				_VergiDairesiRepo.Add(newEntry);
			}
			else
			{
				//model.CreateUserID = _userId;
				//model.IsActive = (bool)existingEntry.IsActive;
				//model.IsDelete = (bool)existingEntry.IsDelete;
				//model.CreateDate = existingEntry.CreateDate == null ? DateTime.Now : existingEntry.CreateDate;

				_mapper.Map(model, kayit);
				_VergiDairesiRepo.Update(kayit);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<VergiDairesiVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new VergiDairesiVM();
			}

			VergiDairesiVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new VergiDairesiVM();
			}

			return kayit;
		}

		public async Task<List<VergiDairesiVM>> VeriListele(VergiDairesiVM model)
		{
			var liste = GenelListe();

			if (model.Ad != null)
			{
				liste = liste.Where(p => p.Ad.Contains(model.Ad));
			}
			if (model.Ilce != null)
			{
				liste = liste.Where(p => p.Ilce.Contains(model.Ilce));
			}
			if (model.Kod != null)
			{
				liste = liste.Where(p => p.Kod == model.Kod);
			}
			if (model.SehirID != 0)
			{
				liste = liste.Where(p => p.SehirID == model.SehirID);
			}
			var donus = await liste.OrderBy(a => a.ID).Take(1000).ToListAsync();
			return donus;
		}

		public async Task<List<VergiDairesiVM>> VeriListele()
		{
			try
			{
				var List = await GenelListe().OrderBy(a => a.ID)
								  .Take(1000)
								  .ToListAsync();
				return List;
			}
			catch (Exception ex)
			{

				return new List<VergiDairesiVM>();
			}
		}

		public async Task<bool> VeriSil(VergiDairesiVM model)
		{
			VergiDairesi? kayit = _VergiDairesiRepo.GetById(model.ID);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_VergiDairesiRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public Task<bool> VeriSil(int id)
		{
			throw new NotImplementedException();
		}
	}
}
