using Ekomers.Common.Services;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Ekomers.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

// Add the following using directive at the top of the file to resolve the 'Image' type:
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
//using static System.Net.Mime.MediaTypeNames;

namespace Ekomers.Web.Controllers
{
    [TypeFilter(typeof(ErrorFilter))]
    public class HomeController : BaseController
    {
        private SignInManager<Kullanici> _signInManager { get; }
		private readonly ISmsSender _smsSender;
		private readonly IEmailSenderService _emailSender;
		private IUserService _userService { get; set; }
		private readonly IPortalMenuService _menuService;
		private readonly ApplicationDbContext _context; 

        public HomeController(ApplicationDbContext context,UserManager<Kullanici> userManager, IUserService userService,
            SignInManager<Kullanici> signInManager, RoleManager<Rol> rolManager, ISmsSender smsSender,IEmailSenderService emailSender
            , IPortalMenuService menuService
			) : base(userManager, rolManager)
        {
            _signInManager = signInManager;
            _userService = userService;
            _context = context; 
            _smsSender= smsSender;
            _emailSender = emailSender;
            _menuService = menuService;
        }

        public IActionResult Index()
        {
			return RedirectToAction(nameof(HomeController.SignIn));
		}

        public IActionResult SignUp()
        {
			ViewBag.IlgiliMudurlukListe = _context.Departman.Where(p => p.IsActive == true && p.IsDelete == false).OrderBy(p => p.Ad).ToList();
			ViewBag.SirketListe = _context.Sirketler.Where(p => p.IsActive == true && p.IsDelete == false).OrderBy(p => p.SirketAdi).ToList();

			return View();
        }
		private void AddErrors(IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError(string.Empty, error.Description);
			}
		}
		[HttpPost]
        public async Task<IActionResult> SignUp(RegisterVM model)
        {
			if (string.IsNullOrWhiteSpace(model.Password) || !IsStrongPassword(model.Password))
			{
				ModelState.AddModelError(nameof(model.Password),
					"Şifre en az 8 karakter olmalı; büyük harf, küçük harf, rakam ve özel karakter içermelidir.");
			}

			// (Varsa) Şifre tekrar kontrolü
			if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
			{
				ModelState.AddModelError(nameof(model.ConfirmPassword), "Şifre ve şifre tekrarı uyuşmuyor.");
			}

			if (ModelState.IsValid)
            {

         
				var identRes = await _userManager.CreateAsync(new Kullanici
                {
                    UserName = model.UserName,
                    AdSoyad = model.AdSoyad!,
                    PhoneNumber = model.PhoneNumber?.Replace(" ", ""),
                    Email = model.Email,
                    DepartmanID= model.DepartmanID,
                    SirketID= model.SirketID,
                    IsActive=true,
                }, model.Password);

                if (identRes.Succeeded)
                {
                    TempData["Message"] = "Üyelik işlemi başarıyla tamamlanmıştır.";
                    return RedirectToAction(nameof(HomeController.SignUp));
                }

                foreach (IdentityError item in identRes.Errors)
                {
                    var hata = "Geçersiz karakter veya daha önceden alınmış bir kullanıcı adını girdiniz. Kullanıcı adınızı kontrol ediniz!";
                    ModelState.AddModelError(String.Empty, hata /*item.Description*/);
                }
            }
			 
			ModelState.AddModelError(string.Empty, "Üyelik işlemi sırasında hata oluştu.");

			//TempData["MessageError"] = "Üyelik işlemi sırasında hata oluştu.";
			ViewBag.IlgiliMudurlukListe = _context.Departman.Where(p => p.IsActive == true && p.IsDelete == false).OrderBy(p => p.Ad).ToList();
			ViewBag.SirketListe = _context.Sirketler.Where(p => p.IsActive == true && p.IsDelete == false).OrderBy(p => p.SirketAdi).ToList();

			return View(model);
		}

		private static bool IsStrongPassword(string password)
		{
			// En az 8 karakter, en az 1 küçük, 1 büyük, 1 rakam ve 1 özel karakter
			// Özel karakter: [^\da-zA-Z] = harf/rakam dışı herhangi bir karakter
			var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$";
			return System.Text.RegularExpressions.Regex.IsMatch(
				password,
				pattern,
				System.Text.RegularExpressions.RegexOptions.CultureInvariant
			);
		}

		public IActionResult SignIn()
		{
            if (User.Identity.IsAuthenticated)
            {
				return RedirectToAction(nameof(HomeController.Member));
			}
			return View();
		}
		//private async Task HandleUserLoginEvent(string username)
  //      {
  //          // Kullanıcı oturum açtığında yapılacak işlemleri burada gerçekleştirin
  //          // Örneğin, tüm kullanıcılara bir bildirim gönderme gibi
            
  //      }
        [HttpPost]
        public async Task<IActionResult> SignIn(SignInVM model, string? returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Action("Member", "Home");

            var user = await _userManager.FindByEmailAsync(model.Email);
			 
			HttpContext.Session.SetString("user", JsonSerializer.Serialize(user));		 
					 
			
            
            if (user == null)
            {
                ModelState.AddModelError(String.Empty, "Eposta veya şifre yanlış");
                return RedirectToAction(nameof(HomeController.SignIn));
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, false, true);

            if (signInResult.Succeeded)
            {
				List<PortalMenuTreeItem> MenuTree = _menuService.GetMenuTree();
				// JSON formatına çevirme
				string menuJson = JsonSerializer.Serialize(MenuTree);

				// Session'a ekleme
				HttpContext.Session.SetString("Menu", menuJson);
				// await HandleUserLoginEvent(user.UserName);
				//_userConnectionService.AddConnection(user.UserName, user.Id);
				var claims = new List<Claim>
                {
                    new("AdSoyad", user.AdSoyad)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "login");
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(claimsIdentity));
                user.SonGirisTarihi = DateTime.Now;
                await _userManager.UpdateAsync(user);
                _userService.AddUserActivityLog("Home", "SignIn", user.Email, "UserLoggedIn", user.UserName);
                return Redirect(returnUrl);
            }

            if(signInResult.IsLockedOut)
            {
                ModelState.AddModelError(String.Empty, "Hesap kilitlendi, 3 dakika boyunca giriş yapamazsınız.");
                return View();
            }
            
            ModelState.AddModelError(string.Empty, "Eposta veya şifre yanlış");
            return RedirectToAction(nameof(HomeController.SignIn));
        }

        public async Task<IActionResult> LogOut()
        {
            if (User.Identity.IsAuthenticated == true)
            {
                var user = await _userManager.FindByNameAsync(User.Identity!.Name);
                _userService.AddUserActivityLog("Home", "LogOut", "", "UserLoggedOut", user.UserName);
                await _signInManager.SignOutAsync();
            }

            return RedirectToAction(nameof(HomeController.SignIn));
        }
        
      


        [Authorize]
        public async Task<IActionResult> Member()
        {
            var user =await  _userManager.FindByNameAsync(User.Identity!.Name);
            return View(user);
        }
	 
		//public IActionResult ForgetPassword()
		//{
		//	return View();
		//}
		//[HttpPost]
		//      public async Task<IActionResult> ForgetPassword(string Email)
		//      {
		//          var user = await _userManager.FindByEmailAsync(Email);

		//          if (user == null)
		//          {
		//              ModelState.AddModelError(String.Empty, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.");
		//              return View();
		//          }

		//          string passResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
		//          string passResetLink = Url.Action("ResetPassword", "Home", new {userId = user.Id, Token = passResetToken}, HttpContext.Request.Scheme);
		//          //Örnek URL: http://192.168.11.234:85/Home/ResetPassword?Id=....&Token=...

		//          TempData["Success"] = "Şifre sıfırlama bağlantısı eposta adresinize gönderilmiştir.";
		//          return RedirectToAction(nameof(ForgetPassword));
		//      }

		[Authorize]
        public async Task<IActionResult> Profile ()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            var vm = new SignUpVM()
            {
                UserName = user.UserName,
                AdSoyad = user.AdSoyad,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Bolum= user.Bolum,
                Unvan= user.Unvan,
                DepartmanID=user.DepartmanID,
                SirketID=user.SirketID
            };
			ViewBag.IlgiliMudurlukListe = _context.Departman.Where(p => p.IsActive == true && p.IsDelete == false).OrderBy(p => p.Ad).ToList();
			ViewBag.SirketListe = _context.Sirketler.Where(p => p.IsActive == true && p.IsDelete == false).OrderBy(p => p.SirketAdi).ToList();
			return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Profile(SignUpVM model)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            
            user.PhoneNumber = model.PhoneNumber;
            user.AdSoyad = model.AdSoyad;
            user.Bolum = model.Bolum;
            user.Unvan = model.Unvan;
            user.SirketID = model.SirketID;
            user.DepartmanID = model.DepartmanID;
            if(model.Email != user.Email)
            {
                await _userManager.SetEmailAsync(user, model.Email);
            }
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(HomeController.Member));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            //return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature != null)
            {
                var exception = exceptionHandlerPathFeature.Error;
                // İsterseniz burada loglama yapabilirsiniz.
                return View("Error", exception);
            }
            return View("Error");
        }
        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }




        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadAndSave(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                int maxFileSizeInMb = 4;
                int maxFileSizeInBytes = maxFileSizeInMb * 1024 * 1024;

                if (file.Length > maxFileSizeInBytes)
                {
                    ViewBag.Message = "Dosya boyutu çok büyük. Maksimum dosya boyutu: " + maxFileSizeInMb + " MB.";
                    return View("Index");
                }

                byte[] resizedImageBytes;
                using (var stream = file.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                   resizedImageBytes = ResizeImage(memoryStream, 400, 400);
                }

                IImageProcessor imageProcessor;

                //if (saveToDatabase)
                //{
                 //    imageProcessor = new DatabaseImageProcessor(_context);
                //}
                //else
                //{
                   imageProcessor = new FolderImageProcessor();
                //}

                string result = imageProcessor.ProcessImage(resizedImageBytes, "wwwroot/img/user", User.Identity.Name);
                var userImage = new UserImage();
                //userImage.ImageId=

                //var imageModel = _context.UserImage.Add(imageId);

                ViewBag.Message = "Fotoğraf başarıyla işlendi. Sonuç: " + result;
            }
            else
            {
                ViewBag.Message = "Lütfen bir dosya seçin.";
            }
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            //return View(user);
            return View("Member", user);
        }

        private byte[] ResizeImage(Stream imageStream, int width, int height)
        {
            using (System.Drawing.Image image = Image.FromStream(imageStream))
            using (System.Drawing.Image resizedImage = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                graphics.DrawImage(image, 0, 0, width, height);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    resizedImage?.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return memoryStream.ToArray();
                }
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ShowImage(int imageId)
        {
            var imageModel = _context.UserImage.Find(imageId);

            if (imageModel == null)
            {
                return NotFound(); // Eğer resim bulunamazsa 404 hatası döndürülebilir.
            }

            return File(imageModel.ImageData, "image/jpeg"); // Veritabanından alınan resmi HTTP yanıtında gönder.
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SifreDegistir(SifreDegistirVM model)
        {
            // Şifrelerin uyuşup uyuşmadığını kontrol ederiz

            if (model.Password1 != model.Password2)
            {
                // Eğer şifreler uyuşmuyorsa, hata mesajı ekleriz
                TempData["MessageError"] = "Girilen şifreler aynı değil!";
                return RedirectToAction("Profile", "Home"); // Kullanıcıyı anasayfaya yönlendiririz
            }

            // Şifre değiştirme işlemi burada yapılır.
            // Örneğin, kullanıcıyı veritabanından alıp şifresini güncellemek:
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                // Şifre doğrulaması yapmak için PasswordValidator kullanıyoruz
                var passwordValidator = new PasswordValidator<Kullanici>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, user, model.Password1);

                if (passwordValidationResult.Succeeded)
                {
                    // Şifre kurallarına uygun olduğu için eski şifreyi kaldırıp yenisini ekliyoruz
                    await _userManager.RemovePasswordAsync(user);  // Mevcut şifreyi kaldır
                    var result = await _userManager.AddPasswordAsync(user, model.Password1); // Yeni şifreyi ekle

                    if (result.Succeeded)
                    {
                        TempData["Message"] = "Şifre başarıyla değiştirildi.";
                        return RedirectToAction("Profile", "Home");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description); // Hataları gösteririz
                        }
                    }
                }
                else
                {
                    // Şifre güvenlik kurallarına uymuyorsa hataları gösteririz
                    foreach (var error in passwordValidationResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Kullanıcı bulunamadı.");
            }


            // Eğer validation başarısızsa, formu yeniden gösteririz
            TempData["MessageError"] = "Şifre değiştirilemedi! Güvenli şifre giriniz!";
            return RedirectToAction("Profile", "Home"); // Kullanıcıyı anasayfaya yönlendiririz
        }

		async Task<bool> SmsSend(string phoneNumber, string adSoyad, string newPass)
		{
			// StringBuilder nesnesi oluşturma
			var textBuilder = new StringBuilder();

			// Metin parçaları ekleme
			textBuilder.Append("Sayın " + adSoyad + ", ");
			textBuilder.Append("Geçici şifreniz : " + newPass);

			// Yeni satır ekleme
			textBuilder.AppendLine();
			textBuilder.AppendLine("Giriş yaptıktan sonra şifrenizi değiştiriniz!");

			// Değerin son hali
			string smsText = textBuilder.ToString();

			bool result = await _smsSender.SendSmsAsync(phoneNumber, smsText);

			if (result)
			{
				return true;
			}
			return false;
		}
		async Task<bool> EmailSend(string email, string adSoyad, string newPass)
		{
			// StringBuilder nesnesi oluşturma
			var textBuilder = new StringBuilder();

			// Metin parçaları ekleme
			textBuilder.Append("Sayın " + adSoyad + ", ");
			textBuilder.Append("Geçici şifreniz : " + newPass);

			// Yeni satır ekleme
			textBuilder.AppendLine();
			textBuilder.AppendLine("Giriş yaptıktan sonra şifrenizi değiştiriniz!");

			// Değerin son hali
			string smsText = textBuilder.ToString();

			bool result = await _emailSender.SendEmailAsync(email,"Şifre Hatırlatma", smsText);

			if (result)
			{
				return true;
			}
			return false;
		}
		public IActionResult ForgotPassword()
		{
			return View();
		}
		public static bool TelefonNumarasiDogrula(string telefonNumarasi)
		{
			// Telefon numarasındaki boşlukları kaldırıyoruz
			string temizNumara = Regex.Replace(telefonNumarasi, @"\s+", ""); // Tüm boşlukları kaldır

			// Numara 0 ile başlamalı, 5 ile devam etmeli ve toplamda 11 rakam olmalı
			string pattern = @"^05[0-9]{9}$";

			// Regex ile kontrol et ve sonucu döndür
			return Regex.IsMatch(temizNumara, pattern);
		}
		[HttpPost]
		public async Task<IActionResult> _ForgotPassword(string Email)
		{
			var user = await _userManager.FindByEmailAsync(Email);

			if (user == null)
			{
				//_userService.AddSystemActivityLog((int)EnumLogCategory.Information, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.", "Kullanıcı Adı hatalı");
				ModelState.AddModelError(String.Empty, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.");
                

                return View();
			}
             
			if (user.PhoneNumber != null && TelefonNumarasiDogrula(user.PhoneNumber) ==true)
			{
				var newPass = SifreOlustur();
				//var SendOk = await SmsSend(user.PhoneNumber, user.AdSoyad, newPass);
				var SendOk = await EmailSend(user.PhoneNumber, user.AdSoyad, newPass);
				if (SendOk)
				{
					await _userManager.RemovePasswordAsync(user);  // Mevcut şifreyi kaldır
					var result = await _userManager.AddPasswordAsync(user, newPass); // Yeni şifreyi ekle
				}
               
				//_userService.AddSystemActivityLog((int)EnumLogCategory.Information, "Geçici şifreniz telefon numaranıza gönderilmiştir.", user.UserName);
				TempData["Success"] = "Geçici şifreniz email adresinize gönderilmiştir. Giriş yaptıktan sonra şifrenizi değiştiriniz.";


				return View();
			}
			else
			{
				//_userService.AddSystemActivityLog((int)EnumLogCategory.Information, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.", "Kullanıcı Adı hatalı");
				ModelState.AddModelError(String.Empty, "Gönderim sırasında hata olmuştur. Sistemdeki bilgilerinizi kontrol ettiriniz!");
                TempData["Error"] = "Yazılım departmanını arayınız. 0 506 671 93 70 ";
                return View();

			}
			 
		}
		[HttpPost]
		public async Task<IActionResult> ForgotPassword(string Email)
		{
			var user = await _userManager.FindByEmailAsync(Email);

			if (user == null)
			{
				//_userService.AddSystemActivityLog((int)EnumLogCategory.Information, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.", "Kullanıcı Adı hatalı");
				ModelState.AddModelError(String.Empty, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.");


				return View();
			}

			if (user.Email != null  )
			{
				var newPass = SifreOlustur();
				var SendOk = await EmailSend(user.Email, user.AdSoyad, newPass);
				if (SendOk)
				{
					await _userManager.RemovePasswordAsync(user);  // Mevcut şifreyi kaldır
					var result = await _userManager.AddPasswordAsync(user, newPass); // Yeni şifreyi ekle
				}

				//_userService.AddSystemActivityLog((int)EnumLogCategory.Information, "Geçici şifreniz telefon numaranıza gönderilmiştir.", user.UserName);
				TempData["Success"] = "Geçici şifreniz email adresinize gönderilmiştir. Giriş yaptıktan sonra şifrenizi değiştiriniz.";


				return View();
			}
			else
			{
				//_userService.AddSystemActivityLog((int)EnumLogCategory.Information, "Bu eposta adresine kayıtlı kullanıcı bulunamamıştır.", "Kullanıcı Adı hatalı");
				ModelState.AddModelError(String.Empty, "Gönderim sırasında hata olmuştur. Sistemdeki bilgilerinizi kontrol ettiriniz!");
				TempData["Error"] = "Yazılım departmanını arayınız. 0 506 671 93 70 ";
				return View();

			}

		}
		static string SifreOlustur()
		{
			var random = new Random();

			// 4 harf oluştur (küçük veya büyük harf)
			var harfler = new string(Enumerable.Repeat("ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz", 5)
										.Select(s => s[random.Next(s.Length)]).ToArray());

			var karakter = new string(Enumerable.Repeat("!+%&/()=?*", 1)
										.Select(s => s[random.Next(s.Length)]).ToArray());

			// 2 sayı oluştur
			var sayilar = new string(Enumerable.Repeat("0123456789", 2)
										.Select(s => s[random.Next(s.Length)]).ToArray());

			// Harfleri ve sayıları birleştir
			string sifre = harfler + sayilar+ karakter;

			// Şifrenin karışık olmasını sağla
			sifre = new string(sifre.ToCharArray().OrderBy(s => random.Next()).ToArray());

			return sifre;
		}
		public IActionResult Deneme()
		{
			return View();
		}
	}
}