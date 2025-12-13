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
using Ekomers.Models.Entity;

namespace Ekomers.Data.Services
{  
		public class SirketlerService : ISirketlerService
		{
			private readonly ApplicationDbContext _context;
			private readonly IHttpContextAccessor _httpContextAccessor;
			private readonly IMapper _mapper;

			private readonly IRepository<Kullanici> _userRepo;
			private readonly IRepository<Sirketler> _SirketlerRepo;
			 
			private readonly ClaimsPrincipal _user;
			private readonly string _userId;
			public SirketlerService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
				, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<Sirketler> SirketlerRepo
				, IRepository<Departman> departmanRepo, IRepository<Mahalle> mahalleRepo 
				 
				)
			{
				_context = context;
				_httpContextAccessor = httpContextAccessor;
				_mapper = mapper;

				_SirketlerRepo = SirketlerRepo;
				_userRepo = userRepo;
			 
				// Get the current user's claims principal and user ID
				_user = _httpContextAccessor.HttpContext?.User;
				_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			}
		public IQueryable<SirketlerVM> GenelListe()
		{
			Expression<Func<Sirketler, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _SirketlerRepo.GetAll2(filter)

						 



						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new SirketlerVM
						 {
							 ID = kayit.ID,
							 SirketAdi = kayit.SirketAdi,
							 SirketYetkili = kayit.SirketYetkili,
							 SirketYetkiliTel = kayit.SirketYetkiliTel,
							 SirketYetkiliEmail = kayit.SirketYetkiliEmail,
							 SirketAdres = kayit.SirketAdres,
							 SirketVergiDairesi = kayit.SirketVergiDairesi,
							 SirketVergiNo = kayit.SirketVergiNo,
							 SirketWebSitesi = kayit.SirketWebSitesi,
							 SirketLogo = kayit.SirketLogo,
							 LogoTigerSirketKodu = kayit.LogoTigerSirketKodu,
							 Aciklama = kayit.Aciklama,



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

		 

		public Task<SirketlerVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(SirketlerVM model)
		{
			model.Aciklama = model.Aciklama.Replace("\r\n", "");
			model.SirketAdres = model.SirketAdres.Replace("\r\n", "");
			Sirketler? existingEntry = _SirketlerRepo.GetById(model.ID);
			if (existingEntry == null)
			{

				var newEntry = _mapper.Map<Sirketler>(model);


				_SirketlerRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_SirketlerRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<SirketlerVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new SirketlerVM();
			}

			SirketlerVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new SirketlerVM();
			}

			return kayit;
		}

		public async Task<List<SirketlerVM>> VeriListele(SirketlerVM model)
		{
			var liste = GenelListe();

			 

			if (model.Aciklama != null)
			{
				liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
				p.SirketAdi.Contains(model.Aciklama) ||
				p.SirketYetkili.Contains(model.Aciklama) ||
				p.SirketYetkiliTel.Contains(model.Aciklama) ||
				p.SirketYetkiliEmail.Contains(model.Aciklama) ||
				p.SirketVergiDairesi.Contains(model.Aciklama) ||
				p.SirketVergiNo.Contains(model.Aciklama) ||
				p.SirketWebSitesi.Contains(model.Aciklama) || 

				p.SirketAdres.Contains(model.Aciklama));
			}
			 
			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<SirketlerVM>> VeriListele()
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

				return new List<SirketlerVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			Sirketler? kayit = _SirketlerRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_SirketlerRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}
	}
}
