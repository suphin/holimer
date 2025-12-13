using Ekomers.Common.Services;
using Ekomers.Data;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class CrmController : BaseController
	{
		private SignInManager<Kullanici> _signInManager { get; }
		private readonly ISmsSender _smsSender;
		private IUserService _userService { get; set; }
		private readonly IPortalMenuService _menuService;
		private readonly ApplicationDbContext _context;
		private readonly IMusterilerService _musterilerService;

		public CrmController(ApplicationDbContext context, UserManager<Kullanici> userManager, IUserService userService,
			SignInManager<Kullanici> signInManager, RoleManager<Rol> rolManager, ISmsSender smsSender
			, IPortalMenuService menuService
			, IMusterilerService musterilerService
			) : base(userManager, rolManager)
		{
			_signInManager = signInManager;
			_userService = userService;
			_context = context;
			_smsSender = smsSender;
			_menuService = menuService;
		_musterilerService = musterilerService;
		}
		public IActionResult Index()
		{
			ViewBag.Modul = "CRM";
			return View();
		}


	 
		public async Task<IActionResult> Kullanicilar()
		{
			var lst = new List<UserListVM>();
			foreach (var user in await _userManager.Users.Where(P => P.IsCrmUser).OrderBy(a => a.AdSoyad).ToListAsync())
			{
				IList<string> tmp = await _userManager.GetRolesAsync(user);
			
				lst.Add(new UserListVM()
				{
					Id = user.Id,
					Email = user.Email,
					AdSoyad = user.AdSoyad,
					UserName = user.UserName,
					Telefon = user.PhoneNumber,
					SonGirisTarihi = user.SonGirisTarihi
				});
			}
			return View(lst);
		}



		public async Task<IActionResult> Hedefler(int yil = 2025)
		{ 
			ViewBag.Modul = "Tanimlamalar";
			// Kullanıcı listesi (örnek)
			var users = await _userManager.Users.Where(P=>P.IsCrmUser).OrderBy(a => a.AdSoyad).ToListAsync();

			var hedefler = await _context.CrmHedefler
				.Where(h => h.Yil == yil)
				.ToListAsync();

			var model = new HedefTabloVM
			{
				Yil = yil,
				KullaniciHedefleri = users.Select(u => new KullaniciHedefVM
				{
					UserID = u.Id,
					UserName = u.UserName,
					Hedefler = Enumerable.Range(1, 12).Select(ay =>
					{
						var h = hedefler.FirstOrDefault(x => x.UserID == u.Id && x.Ay == ay);
						return new HedefCellVM
						{
							Ay = ay,
							Miktar = h?.Miktar,
							Tutar = h?.Tutar
						};
					}).ToList()
				}).ToList()
			};

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Kaydet(HedefTabloVM model)
		{
			foreach (var k in model.KullaniciHedefleri)
			{
				foreach (var h in k.Hedefler)
				{
					var mevcut = await _context.CrmHedefler
						.FirstOrDefaultAsync(x => x.UserID == k.UserID && x.Yil == model.Yil && x.Ay == h.Ay);

					if (mevcut == null)
					{
						mevcut = new CrmHedefler
						{
							UserID = k.UserID,
							Yil = model.Yil,
							Ay = h.Ay
						};
						_context.CrmHedefler.Add(mevcut);
					}

					mevcut.Miktar = h.Miktar ?? 0;
					mevcut.Tutar = h.Tutar ?? 0;
				}
			}

			await _context.SaveChangesAsync();
			TempData["Message"] = "Hedefler kaydedildi";
			return RedirectToAction(nameof(Hedefler), new { yil = model.Yil });
		}



		//public async Task<PartialViewResult> IliskiMusteri()
		//{
		//	ViewBag.Modul = "CRM"; 

		//	var model = new MusterilerVM
		//	{
		//		MusterilerVMListe = await _musterilerService.VeriListele() 
		//	};

		//	return PartialView("_Musteriler",model);
		//}

		public async Task<IActionResult> IliskiMusteri(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = "CRM"; 
			var paged = await _musterilerService.VeriListeleAsync(page, pageSize, ct);

			var model = new MusterilerVM
			{
				MusterilerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return PartialView("_Musteriler", model);
		}


	}
}
