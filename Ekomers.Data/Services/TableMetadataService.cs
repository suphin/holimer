 
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
	public class TableMetadataService : ITableMetadataService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IMapper _mapper;

		private readonly IRepository<Kullanici> _userRepo;
		private readonly IRepository<TableMetadata> _TableMetadataRepo;
		 
		  
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;
		public TableMetadataService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor
			, IMapper mapper, IRepository<Kullanici> userRepo, IRepository<TableMetadata> TableMetadataRepo
			, IRepository<Departman> departmanRepo 
			 
			)
		{
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_mapper = mapper; 
			_TableMetadataRepo = TableMetadataRepo;
			_userRepo = userRepo;

			// Get the current user's claims principal and user ID
			_user = _httpContextAccessor.HttpContext?.User;
			_userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
			 
		}
		public IQueryable<TableMetadataVM> GenelListe()
		{
			Expression<Func<TableMetadata, bool>> filter;
			if (_user.IsInRole("Admin"))
			{
				filter = a => a.IsActive == true;
			}
			else
			{
				filter = a => a.IsActive == true && a.IsDelete == false;
			}
			var result = from kayit in _TableMetadataRepo.GetAll2(filter)



						


						 join createUser in _userRepo.GetAll2() on kayit.CreateUserID equals createUser.Id
						 into createUserGroup
						 from createUser in createUserGroup.DefaultIfEmpty()

						 join deleteUser in _userRepo.GetAll2() on kayit.DeleteUserID equals deleteUser.Id
						 into deleteUserGroup
						 from deleteUser in deleteUserGroup.DefaultIfEmpty()

						 join updateUser in _userRepo.GetAll2() on kayit.UpdateUserID equals updateUser.Id
						 into updateUserGroup
						 from updateUser in updateUserGroup.DefaultIfEmpty()

						 select new TableMetadataVM
						 {
							 ID = kayit.ID,
							






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

		public async Task<List<TableMetadataVM>> TakvimVeriListele()
		{
			try
			{
				var liste = GenelListe();


				var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
				return donus;
			}
			catch (Exception ex)
			{

				return new List<TableMetadataVM>();
			}
		}

		public Task<TableMetadataVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}

		public bool VeriEkle(TableMetadataVM model)
		{
			TableMetadata? existingEntry = _TableMetadataRepo.GetById(model.ID);
			if (existingEntry == null)
			{

				var newEntry = _mapper.Map<TableMetadata>(model);


				_TableMetadataRepo.Add(newEntry);
			}
			else
			{
				_mapper.Map(model, existingEntry);
				_TableMetadataRepo.Update(existingEntry);
			}

			_context.SaveChanges();
			return true;
		}

		public async Task<TableMetadataVM> VeriGetir(int id)
		{
			if (id <= 0)
			{
				return new TableMetadataVM();
			}

			TableMetadataVM kayit = GenelListe().Where(p => p.ID == id).FirstOrDefault();
			if (kayit == null)
			{
				return new TableMetadataVM();
			}

			return kayit;
		}

		public async Task<List<TableMetadataVM>> VeriListele(TableMetadataVM model)
		{
			var liste = GenelListe();

			//if (model.KurumTuruID != 0)
			//{
			//	liste = liste.Where(p => p.KurumTuruID == model.KurumTuruID);
			//}
			//if (model.EgitimTuruID != 0)
			//{
			//	liste = liste.Where(p => p.EgitimTuruID == model.EgitimTuruID);
			//}
			//if (model.EgitmenID != 0)
			//{
			//	liste = liste.Where(p => p.EgitmenID == model.EgitmenID);
			//}
			//if (model.Aciklama != null)
			//{
			//	liste = liste.Where(p => p.Aciklama.Contains(model.Aciklama) ||
			//	p.EgitimKonusu.Contains(model.Aciklama) ||
			//	p.EgitimTuru.Contains(model.Aciklama) ||
			//	p.EgitimVerilenKurum.Contains(model.Aciklama) ||
			//	p.Egitmen.Contains(model.Aciklama) ||
			//	p.KurumTuru.Contains(model.Aciklama) ||
			//	p.Adres.Contains(model.Aciklama));
			//}
			var donus = liste.OrderByDescending(a => a.ID).Take(1000).ToList();
			return donus;
		}

		public async Task<List<TableMetadataVM>> VeriListele()
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

				return new List<TableMetadataVM>();
			}
		}



		public async Task<bool> VeriSil(int id)
		{
			TableMetadata? kayit = _TableMetadataRepo.GetById(id);
			if (kayit != null)
			{
				kayit.DeleteDate = DateTime.Now;
				kayit.IsDelete = true;
				kayit.DeleteUserID = _userId;
				_TableMetadataRepo.Update(kayit);
				await _context.SaveChangesAsync();
			}
			return true;
		}


		#region "Raporlar"




		#endregion

	}
}