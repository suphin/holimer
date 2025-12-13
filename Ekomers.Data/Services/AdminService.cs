using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class AdminService : IAdminService
	{
		private readonly UserManager<Kullanici> _userManager;
        public AdminService(UserManager<Kullanici> userManager)
        {
			_userManager = userManager;
		}
        public async Task<List<UserListVM>> UserList()
		{
			List<UserListVM> lst = new List<UserListVM>();
			foreach (var user in await _userManager.Users.Where(a=>a.IsActive==true).ToListAsync())
			{ 
				lst.Add(new UserListVM()
				{
					Id = user.Id,
					Email = user.Email,
					AdSoyad = user.AdSoyad,
					UserName = user.UserName,
					Telefon = user.PhoneNumber,
					SonGirisTarihi = user.SonGirisTarihi,
					ImageID=user.ImageID,
					Departman=user.Departman,	
					Unvan=user.Unvan,
					IsCrmUser=user.IsCrmUser,

				});
			}
			return  lst ;
		}

		
	}
}
