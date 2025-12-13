using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Ekomers.Data;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Ekomers.Models.ViewModels.Admin;
 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IYetkilendirmeService _yetkilendirmeService;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		protected SignInManager<Kullanici> _signInManager { get; }
		public AdminController(ApplicationDbContext context, UserManager<Kullanici> userManager,
            SignInManager<Kullanici> signInManager, RoleManager<Rol> rolManager
            ,IYetkilendirmeService yetkilendirmeService
            , ICacheService<Kullanici> kullaniciCache
			)
            : base(userManager, rolManager)
        {
            _context = context;
            _signInManager = signInManager;
            _yetkilendirmeService=yetkilendirmeService;
            _kullaniciCache = kullaniciCache;
		}

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult YeniTasarim()
        {
            return View();
        }
		public IActionResult YeniTasarim2()
		{
			return View();
		}

		public async Task<IActionResult> UserList()
        {
           var lst = new List<UserListVM>();
            foreach(var user in await _userManager.Users.OrderBy(a=>a.AdSoyad).ToListAsync())
            {
                IList<string> tmp = await _userManager.GetRolesAsync(user);
                string roller = string.Empty;
                tmp.ToList().ForEach(x =>
                {
                    roller += $"<span class=\"badge badge-info\">{x}</span>";
                });

                lst.Add(new UserListVM()
                {
                    Id = user.Id,
                    Email = user.Email,
                    AdSoyad = user.AdSoyad,
                    UserName = user.UserName,
                    Telefon = user.PhoneNumber,
                    SonGirisTarihi = user.SonGirisTarihi,
                    Roller = roller,
                });
            }
            return View(lst);
        }

        public IActionResult UserDetail(string? Id)
        {
            var user = _userManager.Users.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.Error = string.Empty;
            if (user == null)
            {
                ViewBag.Error = "Kullanıcı Bulunamadı";
                return View();
            }
			ViewBag.IlgiliMudurlukListe = _context.Departman.ToList(); 
            ViewBag.SirketListe = _context.Sirketler.ToList();

            UserDetailVM ud = new UserDetailVM()
            {
                Id = user.Id,
                AdSoyad = user.AdSoyad,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                Departman=user.Departman,
                Unvan=user.Unvan,
                Bolum=user.Bolum,
                SirketID=user.SirketID,
                DepartmanID=user.DepartmanID,
                IsCrmUser=user.IsCrmUser,
                IsEticaretUser=user.IsEticaretUser,
                IsMhUser=user.IsMhUser,
			};

            return  View(ud);
        }

        [HttpPost]
        public async Task<IActionResult> UserDetail(UserDetailVM model)
        {
              //if(!ModelState.IsValid)
            //{
            //    return View(model);
            //}

            var usr = await _userManager.FindByIdAsync(model.Id);

            usr.UserName = model.UserName;
            usr.AdSoyad = model.AdSoyad;
            usr.Email = model.Email;
            usr.PhoneNumber = model.PhoneNumber;
            usr.Departman = model.Departman;
            usr.Unvan = model.Unvan;
            usr.Bolum = model.Bolum;
            usr.DepartmanID = model.DepartmanID;
            usr.SirketID = model.SirketID;
            usr.IsCrmUser = model.IsCrmUser;
            usr.IsEticaretUser = model.IsEticaretUser;
            usr.IsMhUser = model.IsMhUser;

			var usrUpdateResult = await _userManager.UpdateAsync(usr);

            if (! usrUpdateResult.Succeeded)
            {
                AddModelError(usrUpdateResult);
            }

            if (!String.IsNullOrEmpty(model.Password1) && model.Password1 == model.Password2 && model.Password1.Length > 6)
            {
                await _userManager.RemovePasswordAsync(usr);
                await _userManager.AddPasswordAsync(usr, model.Password2);
            }

            await _userManager.UpdateSecurityStampAsync(usr);
              _kullaniciCache.Remove(CacheKeys.SorumlularAll);
			return RedirectToAction("UserDetail", new {Id= model.Id });
        }

        public IActionResult RoleList()
        {
            return View(_roleManager.Roles.Select(a => new RoleVM()
            {
                ID = a.Id,
                Name = a.Name,
            }).OrderBy(a=>a.Name).ToList());
        }

        public IActionResult RoleCreate()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RoleCreate(RoleVM model)
        {
            var r = new Rol();
            r.Name = model.Name;
            IdentityResult result =_roleManager.CreateAsync(r).Result;
            if(result.Succeeded)
            {
                return RedirectToAction("RoleList");
            }
            else
            {
                AddModelError(result);
            }
            return View(model);
        }

        public async Task<IActionResult> RoleDelete(string ID)
        {
            Rol tmp = await _roleManager.FindByIdAsync(ID);
            if (tmp != null)
            {
                IdentityResult result = _roleManager.DeleteAsync(tmp).Result;
                if (result.Succeeded)
                {
                    return RedirectToAction("RoleList");
                }
                ViewBag.Hata = "Silme işlemi sırasında bir hata oluştu.";
            }
            else
                ViewBag.Hata = "Rol bilgisine ulaşımaladı.";

            return View();
        }

        public IActionResult RoleAssign(string id)
        {
            Kullanici user = _userManager.FindByIdAsync(id).Result;
            ViewBag.UserName = user.UserName + " (" + user.AdSoyad + ")";
            ViewBag.UserId = id;
            IQueryable<Rol> roller = _roleManager.Roles.OrderBy(p=>p.Name);
            List<string> userRoles = _userManager.GetRolesAsync(user).Result as List<string>;

            List<RoleAssignVM> list = new List<RoleAssignVM>();

            foreach(var rol in roller)
            {
                RoleAssignVM r = new RoleAssignVM()
                {
                    RoleId = rol.Id,
                    RoleName = rol.Name,
                };

                if (userRoles != null && userRoles.Contains(rol.Name))
                    r.Checked = true;
                else
                    r.Checked = false;
                list.Add(r);
            }

            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> RoleAssign(List<RoleAssignVM> mdl, string UserId)
        {
            Kullanici user = _userManager.FindByIdAsync(UserId).Result;

            foreach (RoleAssignVM item in mdl)
            {
                if(item.Checked)
                {
                    await _userManager.AddToRoleAsync(user, item.RoleName);
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, item.RoleName);
                }
            }
            return RedirectToAction("UserList");
        }

        public async Task<IActionResult> UserRemove(string Id)
        {
            var usr = await _userManager.FindByIdAsync(Id);
            if (usr == null)
            {
                return View();
            }
            await _userManager.DeleteAsync(usr);
            return RedirectToAction("UserList");
        }


        [HttpGet]
        public async Task<IActionResult> AddRoleClaim(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var model = new AddRoleClaimVM
            {
                RoleId = role.Id,
                RoleName = role.Name,
                ExistingClaims = existingClaims.ToList()
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> AddRoleClaim(AddRoleClaimVM model)
        {
            if (ModelState.IsValid)
            {
                var role = await _roleManager.FindByIdAsync(model.RoleId);
                if (role == null)
                {
                    return NotFound();
                }

                var claim = new Claim(model.ClaimType, model.ClaimValue);
                var result = await _roleManager.AddClaimAsync(role, claim);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(AddRoleClaim), new { roleId = model.RoleId });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            var existingClaims = await _roleManager.GetClaimsAsync(await _roleManager.FindByIdAsync(model.RoleId));
            model.ExistingClaims = existingClaims.ToList();
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> AddUserClaim(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles;

            var model = new AddUserClaimVM
            {
                UserId = user.Id,
                UserName = user.UserName,
                ExistingClaims = existingClaims,
                AllRoles = allRoles,
                UserRoles = roles
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserClaim(AddUserClaimVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                var claim = new Claim(model.ClaimType, model.ClaimValue);
                var result = await _userManager.AddClaimAsync(user, claim);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(AddUserClaim), new { userId = model.UserId });
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            var existingClaims = await _userManager.GetClaimsAsync(await _userManager.FindByIdAsync(model.UserId));
            model.ExistingClaims = existingClaims;

            return View(model);
        }

		public async Task<IActionResult> DeleteClaimFromUser(string userId, string claimType, string claimValue)
		{
			// Kullanıcıyı bul
			var user = await _userManager.FindByIdAsync(userId);

			if (user == null)
			{
				return NotFound();
			}

			// Silinecek claim'i oluştur
			var claim = new Claim(claimType, claimValue);

			// Claim'i kullanıcıdan sil
			var result = await _userManager.RemoveClaimAsync(user, claim);

			if (result.Succeeded)
			{
				//await _signInManager.RefreshSignInAsync(user);
				// Başarılıysa, kullanıcı claim sayfasına geri dön
				return RedirectToAction(nameof(AddUserClaim), new { userId = userId });
			}
			else
			{
				// Hata durumunda hata mesajlarını ekle
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}

			// Hatalı işlem olursa ilgili sayfaya geri dön
			return RedirectToAction(nameof(AddUserClaim), new { userId = userId });
		}
		public async Task<IActionResult> DeleteRoleFromUser(string userId, string roleName)
		{
			// Kullanıcıyı bul
			var user = await _userManager.FindByIdAsync(userId);

			if (user == null)
			{
				return NotFound();
			}

			// Kullanıcıdan rol sil
			var result = await _userManager.RemoveFromRoleAsync(user, roleName);

			if (result.Succeeded)
			{
				// Başarılıysa kullanıcı rol sayfasına geri dön
				//return RedirectToAction(nameof(AddUserRole), new { userId = userId });
				return RedirectToAction(nameof(AddUserClaim), new { userId = userId });
			}
			else
			{
				// Hata durumunda hata mesajlarını ekle
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}

			// Hatalı işlem olursa ilgili sayfaya geri dön
			// Hata varsa aynı sayfayı hata mesajları ile döndür
			return View();

		}

		public async Task<IActionResult> DeleteClaimFromRole(string roleId, string claimType, string claimValue)
		{
			// Rolü bul
			var role = await _roleManager.FindByIdAsync(roleId);

			if (role == null)
			{
				return NotFound();
			}

			// Role atanmış tüm claim'leri al
			var claims = await _roleManager.GetClaimsAsync(role);

			// Silinecek claim'i bul
			var claim = claims.FirstOrDefault(c => c.Type == claimType && c.Value == claimValue);

			if (claim != null)
			{
				// Claim'i role'den sil
				var result = await _roleManager.RemoveClaimAsync(role, claim);

				if (result.Succeeded)
				{
					// Başarılıysa, role detay sayfasına geri dön 
					return RedirectToAction("AddRoleClaim", new { roleId = roleId });
				}
				else
				{
					// Hata durumunda hataları ekle
					foreach (var error in result.Errors)
					{
						ModelState.AddModelError("", error.Description);
					}
				}
			}
			else
			{
				ModelState.AddModelError("", "Claim bulunamadı.");
			}

			// İşlem başarısız olursa, tekrar role detay sayfasına dön
			return RedirectToAction("AddRoleClaim", new { roleId = roleId });
		}


		public async Task<IActionResult> Kopyala(string UserId)
		{
			List<Kullanici> users =await _context.Users.OrderBy(p=>p.AdSoyad).ToListAsync() ;
			var usr = await _userManager.FindByIdAsync(UserId);

			ViewBag.User = usr;
			return PartialView(users);
		}
		[HttpPost]
		public async Task<IActionResult> Kopyala(UserRoleCopy model)
		{
			// Kaynak ve hedef kullanıcıları al
			var sourceUser = await _userManager.FindByIdAsync(model.sourceUserId);
			var targetUser = await _userManager.FindByIdAsync(model.targetUserId);

			if (sourceUser == null || targetUser == null)
			{
				throw new Exception("Kaynak veya hedef kullanıcı bulunamadı.");
			}

			// Kaynak kullanıcının rollerini al
			var sourceRoles = await _userManager.GetRolesAsync(sourceUser);

			// Hedef kullanıcının rollerini al
			var targetRoles = await _userManager.GetRolesAsync(targetUser);

			// Hedef kullanıcıya eklenmesi gereken rolleri belirle
			var rolesToAdd = sourceRoles.Except(targetRoles);

			// Rolleri hedef kullanıcıya ekle
			foreach (var role in rolesToAdd)
			{
				var result = await _userManager.AddToRoleAsync(targetUser, role);
				if (!result.Succeeded)
				{
					throw new Exception($"Rol eklenirken hata oluştu: {role}");
				}
			}

			// Kaynak kullanıcının claim'lerini al
			var sourceClaims = await _userManager.GetClaimsAsync(sourceUser);

			// Hedef kullanıcının claim'lerini al
			var targetClaims = await _userManager.GetClaimsAsync(targetUser);

			// Hedef kullanıcıya eklenmesi gereken claim'leri belirle
			var claimsToAdd = sourceClaims.Except(targetClaims, new ClaimComparer());

			// Claim'leri hedef kullanıcıya ekle
			foreach (var claim in claimsToAdd)
			{
				var result = await _userManager.AddClaimAsync(targetUser, claim);
				if (!result.Succeeded)
				{
					throw new Exception($"Claim eklenirken hata oluştu: {claim.Type} - {claim.Value}");
				}
			}

			return RedirectToAction("UserList");
		}

		public class ClaimComparer : IEqualityComparer<Claim>
		{
			public bool Equals(Claim x, Claim y)
			{
				if (x == null || y == null) return false;
				return x.Type == y.Type && x.Value == y.Value;
			}

			public int GetHashCode(Claim obj)
			{
				return HashCode.Combine(obj.Type, obj.Value);
			}
		}

		public async Task<IActionResult> AddUserAuth(string userId)
		{
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return NotFound();
			}

			var existingClaims = await _userManager.GetClaimsAsync(user);
			var roles = await _userManager.GetRolesAsync(user);
			var allRoles = _roleManager.Roles;
            var yetkiler =  _yetkilendirmeService.GenelListe().ToList();
			var model = new AddUserClaimVM
			{
				UserId = user.Id,
				UserName = user.UserName,
				ExistingClaims = existingClaims,
				AllRoles = allRoles,
				UserRoles = roles ,
                Yetkiler= yetkiler.Select(p => new YetkilendirmeVM
				{
					KategoriID = p.KategoriID,
					KategoriAd = p.KategoriAd,
					Ad = p.Ad,
					ClaimType = p.ClaimType,
					ClaimName = p.ClaimName,
					Selected = existingClaims.Any(c => c.Type == p.ClaimType && c.Value == p.ClaimName)
				}).ToList()
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddUserAuth(AddUserClaimVM model)
		{
			var user = await _userManager.FindByIdAsync(model.UserId);
			if (user == null) return NotFound();

			// Kullanıcının mevcut claim'leri
			var existing = await _userManager.GetClaimsAsync(user);

			// Formdan "seçili" gelen claim'ler
			var selected = model.Yetkiler
				.Where(p => p.Selected)
				.Select(p => new Claim(p.ClaimType, p.ClaimName))
				.ToList();

			// Eklenecekler = selected - existing
			var toAdd = selected
				.Where(sc => !existing.Any(ec => ec.Type == sc.Type && ec.Value == sc.Value))
				.ToList();

			// Kaldırılacaklar = existing - selected (yalnızca katalogdakiler)
			var catalogPairs = model.Yetkiler
				.Select(p => (p.ClaimType, p.ClaimName))
				.ToHashSet();

			var toRemove = existing
				.Where(ec => catalogPairs.Contains((ec.Type, ec.Value))
						  && !selected.Any(sc => sc.Type == ec.Type && sc.Value == ec.Value))
				.ToList();

			foreach (var c in toAdd)
				await _userManager.AddClaimAsync(user, c);

			foreach (var c in toRemove)
				await _userManager.RemoveClaimAsync(user, c);

			TempData["Message"] = "Kullanıcı yetkileri güncellendi.";
			return RedirectToAction(nameof(AddUserAuth), new { userId = model.UserId });
		}

		[HttpGet]
		public async Task<IActionResult> AddRoleAuth(string roleId)
		{
			var role = await _roleManager.FindByIdAsync(roleId);
			if (role == null)
				return NotFound();

			var existingClaims = await _roleManager.GetClaimsAsync(role);

			var yetkiler = _yetkilendirmeService.GenelListe().ToList();

			var model = new AddRoleClaimVM
			{
				RoleId = role.Id,
				RoleName = role.Name, // düzeltildi
				ExistingClaims2 = existingClaims,
				Yetkiler = yetkiler.Select(p => new YetkilendirmeVM
				{
					KategoriID = p.KategoriID,
					KategoriAd = p.KategoriAd,
					Ad = p.Ad,
					ClaimType = p.ClaimType,
					ClaimName = p.ClaimName,
					Selected = existingClaims.Any(c => c.Type == p.ClaimType && c.Value == p.ClaimName)
				}).ToList()
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddRoleAuth(AddRoleClaimVM model)
		{
			var role = await _roleManager.FindByIdAsync(model.RoleId);
			if (role == null)
				return NotFound();

			var existing = await _roleManager.GetClaimsAsync(role);

			var selected = model.Yetkiler
				.Where(p => p.Selected)
				.Select(p => new Claim(p.ClaimType, p.ClaimName))
				.ToList();

			// Eklenecekler = selected - existing
			var toAdd = selected
				.Where(sc => !existing.Any(ec => ec.Type == sc.Type && ec.Value == sc.Value))
				.ToList();

			// Kaldırılacaklar = existing - selected (sadece katalogdakiler)
			var catalogPairs = model.Yetkiler
				.Select(p => (p.ClaimType, p.ClaimName))
				.ToHashSet();

			var toRemove = existing
				.Where(ec => catalogPairs.Contains((ec.Type, ec.Value))
						  && !selected.Any(sc => sc.Type == ec.Type && sc.Value == ec.Value))
				.ToList();

			foreach (var c in toAdd)
				await _roleManager.AddClaimAsync(role, c);

			foreach (var c in toRemove)
				await _roleManager.RemoveClaimAsync(role, c);

			TempData["Message"] = "Rol yetkileri güncellendi.";
			return RedirectToAction(nameof(AddRoleAuth), new { roleId = model.RoleId });
		}


	}
}
