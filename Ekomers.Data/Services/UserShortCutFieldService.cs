using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
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
	public class UserShortCutFieldService : IUserShortCutFieldService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;

		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<UserShortCutField> _UserShortCutFieldRepo;

		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public UserShortCutFieldService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<UserShortCutField> UserShortCutFieldRepo
			, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo

			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper;

			_UserShortCutFieldRepo = UserShortCutFieldRepo;
			_userRepo = userRepo;

			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
		}
		public IQueryable<UserShortCutFieldVM> GenelListe()
		{
			Expression<Func<UserShortCutField, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _UserShortCutFieldRepo.GetAll2(filter)





						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new UserShortCutFieldVM
						 {
							 ID = kayit.ID,
							 Ad = kayit.Ad,
							 Aciklama = kayit.Aciklama,
							 Siralama= kayit.Siralama,


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



		public Task<UserShortCutFieldVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(UserShortCutFieldVM model)
		{
			model.Aciklama = model.Aciklama.Replace("\r\n", ""); 
			UserShortCutField? existingEntry = _UserShortCutFieldRepo.GetById(model.ID);
			if (existingEntry == null)
			{

				var newEntry = _mapper.Map<UserShortCutField>(model);


				_UserShortCutFieldRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_UserShortCutFieldRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<UserShortCutFieldVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new UserShortCutFieldVM();
			}

			UserShortCutFieldVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new UserShortCutFieldVM();
			}

			return kayit;
		}

		public async Task<List<UserShortCutFieldVM>> VeriListele(UserShortCutFieldVM model)
		{
			var liste = GenelListe();



			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.Ad.Contains(model.Aciklama) );
			}

			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<UserShortCutFieldVM>> VeriListele()
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

				return new List<UserShortCutFieldVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			UserShortCutField? kayit = _UserShortCutFieldRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_UserShortCutFieldRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}
	}
}
