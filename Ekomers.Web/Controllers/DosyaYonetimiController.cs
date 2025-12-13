using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Drawing.Imaging;
using System.Reflection;
using System.Security.Claims;
using System;
using System.Drawing; 
using System.IO;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
namespace Ekomers.Web.Controllers
{
   [Authorize(Roles = "Admin,DosyaYonetimi")]
    [TypeFilter(typeof(ActionFilter))]
    [TypeFilter(typeof(ErrorFilter))]
    public class DosyaYonetimiController : BaseController
    {
        

        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IFileService _fileService;
        private readonly FileSettings _fileSettings;
		private readonly List<string> _allowedExtensions;
		private string _userId;
        public DosyaYonetimiController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
             IMemoryCache cache
            , IWebHostEnvironment hostingEnvironment, IFileService fileService
            , IOptions<FileSettings> fileSettings
			) : base(userManager, roleManager)
        {
           
            _cache = cache;
            _hostingEnvironment = hostingEnvironment;
            _fileService = fileService;
			_fileSettings = fileSettings.Value;
			_allowedExtensions = fileSettings.Value.AllowedFileExtensions;
		}


		//public async Task<ActionResult> koordinatBas(int modulID,string modulName)
		//{
		//	var _uploadPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", modulName);
		//	List<DosyaVM> modelc = await _fileService.DosyaGetir(modulID);
  //          foreach ( DosyaVM dosya in modelc)
  //          {
		//		var filePath = Path.Combine(_uploadPath, dosya.DosyaAdi);

		//		var directories = ImageMetadataReader.ReadMetadata(filePath);
		//		var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
  //              if (gpsDirectory != null)
  //              {
		//			dosya.Latitude = gpsDirectory.GetGeoLocation()?.Latitude;
		//			dosya.Longitude = gpsDirectory.GetGeoLocation()?.Longitude;
		//			await _fileService.KoordinatKaydet(dosya) ;
		//		}
		//	}
			
			 
  //          return Ok();
		//}



		public override void OnActionExecuting(ActionExecutingContext context)
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        #region "Dosya yükleme ve getirme işlemleri"
        public async Task<PartialViewResult> DosyaGetir(int VeriID = 0,int ModulID=0,string DosyaYolu="")
        {
            var model = new DosyaYonetimiVM
            {
                DosyaListe = await _fileService.DosyaGetir(VeriID, ModulID),
                KayitID = VeriID,
                DosyaYolu=DosyaYolu
            };
            return PartialView("_DosyaGetir", model);
        }

		public async Task<ActionResult> KlasorGetir(int VeriID = 0, int ModulID = 0, string DosyaYolu = "")
		{
			var model = new DosyaYonetimiVM
			{
				DosyaListe = await _fileService.DosyaGetir(VeriID, ModulID),
				KayitID = VeriID,
				DosyaYolu = DosyaYolu
			}; 

            return PartialView("_KlasorGetir", model);
		}
		public async Task<IActionResult> UpdateIs360(int ID, bool Is360,int ModulID, int KayitID)
		{
			bool sonuc = await _fileService.UpdateIs360(ID,Is360);
            if (sonuc)
            {
				List<DosyaVM> modelc = await _fileService.DosyaGetir((int)KayitID, ModulID);

                 
				return PartialView("DosyaYukle", modelc);
			}
			return BadRequest("Hata oluştu!");
		}
		public IActionResult View360(string filename, string filepath)
		{
			ViewBag.filename = filename;
			ViewBag.filepath = filepath;
			return PartialView("View360");
		}
		public  PartialViewResult  _DosyaKaydet(int VeriID = 0)
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<ActionResult> DosyaYukle(DosyaYonetimiVM model)
        {
            if (model.Dosya != null && model.Dosya.Length > 0)
            {
                var dosya_id = Guid.NewGuid().ToString();
                var fileExtension = Path.GetExtension(model.Dosya.FileName);
				var _uploadPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", model.DosyaYolu);
				var fileName = Path.GetFileName(model.Dosya.FileName);
                var dosyaAdi = dosya_id + fileExtension;
                // var dosyaAdi = dosya_id + (fileExtension.ToLower() == ".tiff" || fileExtension.ToLower() == ".tif" ? ".jpg" : fileExtension);
                var filePath = Path.Combine(_uploadPath, dosyaAdi);

                //  fileExtension = fileExtension.ToLower() == ".tiff" || fileExtension.ToLower() == ".tif" ? ".jpg" : fileExtension;

                // var fileInfo = new FileInfo(fileName);

                if (!System.IO.Directory.Exists(_uploadPath))
                {
                    System.IO.Directory.CreateDirectory(_uploadPath);
                }

                if (_allowedExtensions.Contains(fileExtension.ToLower()))
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Dosya.CopyToAsync(stream);
                    }
                }
                else
                {
                    return BadRequest(new { message = "Dosya uzantısı desteklenmiyor!" });
                    //return BadRequest("Dosya uzantısı desteklenmiyor!");
                    //return PartialView("error",new ErrorViewModel { exception="Dosya uzantısı desteklenmiyor!"});
                }

					var fileInfo = new FileInfo(filePath);
				string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".tiff", ".gif", ".bmp", ".heic" };


				// Eğer dosya bir resim değilse ReadMetadata çağırma
				var directories = imageExtensions.Contains(fileInfo.Extension.ToLower())
					? ImageMetadataReader.ReadMetadata(filePath)
					: new List<MetadataExtractor.Directory>();

				var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

				//var    directories = ImageMetadataReader.ReadMetadata(filePath);
				//  var    gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
                
			   
               
				var yenidosya = new DosyaVM
                {
                    DosyaAdi = dosyaAdi,
                    DosyaUzantisi = fileInfo.Extension,
                    DosyaBoyutu = fileInfo.Length,
                    YuklenmeTarihi = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    OlusturulmaTarihi = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    DegistirilmeTarihi = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    DosyaYolu = fileInfo.FullName,
                    ModulDosyaYolu = model.DosyaYolu,
                    DosyaSahibi = "",//System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                    DosyaTuru = "application/octet-stream", // Gelişmiş MIME türü tespiti yapılabilir
                    FileID = dosya_id, // ID olarak benzersiz bir değer kullanıyoruz
                    FileName = fileName,
                    Is360=false,
                    ModulID = model.ModulID,
                    KayitID = model.KayitID,
                    Aciklama = "??? dosya yükleme işlemi",
					//Latitude = gpsDirectory?.GetGeoLocation()?.Latitude,
					//Longitude = gpsDirectory?.GetGeoLocation()?.Longitude
				};

                bool sonuc = await _fileService.DosyaKaydet(yenidosya);



                List<DosyaVM> modelc = await _fileService.DosyaGetir((int)model.KayitID, model.ModulID);


                //  return Json(modelc);
                return PartialView("DosyaYukle", modelc);
            }
            return BadRequest(new { message = "Dosya yüklenemedi." });
            //  return BadRequest("Dosya yüklenemedi.");
        }

		public async Task<PartialViewResult> DosyaSil(int DosyaID=0, int VeriID = 0, int ModulID = 0, string DosyaYolu = "")
		{
			bool sonuc = await _fileService.DosyaSil(DosyaID);
            List<DosyaVM> modelc = await _fileService.DosyaGetir((int)VeriID, ModulID);


            //  return Json(modelc);
            return PartialView("DosyaYukle", modelc);
        }
		public IActionResult GetPDF(string filepath,string fileName)
        {
            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, filepath, fileName);
            //return PhysicalFile(filePath, "application/pdf");
            //return PhysicalFile(filePath, "application/pdf");
            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            string mimeType;

            switch (fileExtension)
            {
                case ".pdf":
                    mimeType = "application/pdf";
                    break;
                case ".jpg":
                case ".jpeg":
                    mimeType = "image/jpeg";
                    break;
                case ".png":
                    mimeType = "image/png";
                    break;
                case ".gif":
                    mimeType = "image/gif";
                    break;
				case ".mp4":
					mimeType = "video/mp4";
					break;
				case ".avi":
					mimeType = "video/x-msvideo";
					break;
				case ".mov":
					mimeType = "video/quicktime";
					break;
				case ".wmv":
					mimeType = "video/x-ms-wmv";
					break;
				case ".webp":
					mimeType = "image/webp";
					break;
                case ".tif":
                case ".tiff":
                    // TIFF dosyasını JPEG'e dönüştür
                    string jpegFilePath = ConvertTiffToJpeg(filePath);

                    // Eğer dönüştürülmüş JPEG dosyası yoksa, dönüştürme işlemini yap
                    if (!System.IO.File.Exists(jpegFilePath))
                    {
                        jpegFilePath = ConvertTiffToJpeg(filePath);  // Dosya dönüşümünü tekrar yap
                    }

                    // MIME türünü JPEG olarak güncelle
                    mimeType = "image/jpeg";  // JPEG MIME türü

                    // Dosya yolunu JPEG dosyasına güncelle
                    filePath = jpegFilePath;  // JPEG dosyasının yolunu güncelle

                    break;
                //case ".tif":
                //case ".tiff":
                //	//mimeType = "image/tiff";
                //	// TIFF dosyası ise JPEG'e dönüştür
                //	string jpegFilePath = ConvertTiffToJpeg(filePath);
                //	mimeType = "image/jpeg";  // JPEG MIME türü
                //	filePath = jpegFilePath;  // JPEG dosyasının yolunu güncelle
                //	break;
                default:
                    mimeType = "application/octet-stream";
                    break;
            }

            return PhysicalFile(filePath, mimeType);
        }
		public string ConvertTiffToJpeg(string tiffFilePath)
		{
			string jpegFilePath = Path.ChangeExtension(tiffFilePath, ".jpg");

            using (Image tiffImage = Image.FromFile(tiffFilePath))
            {
                tiffImage.Save(jpegFilePath, ImageFormat.Jpeg);
            }

            return jpegFilePath;
		}
		#endregion
	}
	
	public class FileSettings
	{
		public List<string> AllowedFileExtensions { get; set; }
	}
}
