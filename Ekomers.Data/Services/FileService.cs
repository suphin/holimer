using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
    public class FileService  : IFileService 
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
       
        private readonly IRepository<Departman> _departmanRepo;
        private readonly IRepository<Mahalle> _mahalleRepo;
        private readonly IRepository<Dosya> _dosyaRepo;
        private readonly IRepository<Kullanici> _userRepo;
        private readonly ClaimsPrincipal _user;
        private readonly string _userId;
        public FileService(IMapper mapper, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
                    
                    IRepository<Departman> departmanRepo,
                     IRepository<Mahalle> mahalleRepo,
        IRepository<Dosya> dosyaRepo,
                    IRepository<Kullanici> userRepo)
        {
            _httpContextAccessor = httpContextAccessor;
            _user = _httpContextAccessor.HttpContext.User;

            // Kullanıcı ID'sini alma
            _userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _context = context;
            _mapper = mapper;           
            _departmanRepo = departmanRepo;
            _dosyaRepo = dosyaRepo;
            _userRepo = userRepo;
            _mahalleRepo = mahalleRepo;
        }
        #region "Dosya işlemleri"
        public async Task<bool> DosyaKaydet(DosyaVM model)
        {
            var newEntry = _mapper.Map<Dosya>(model);
            newEntry.ModulID = model.ModulID;
            newEntry.CreateDate = DateTime.Now;
            newEntry.IsActive = true;
            newEntry.IsDelete = false;
            newEntry.CreateUserID = _userId;
            _dosyaRepo.Add(newEntry);
            await _context.SaveChangesAsync();
            return true;
        }

		public async Task<bool> KoordinatKaydet(DosyaVM modelview)
		{

			var bilesen = _dosyaRepo.GetById(modelview.ID);

			if (bilesen != null)
			{
				bilesen.Latitude = modelview.Latitude;
				bilesen.Longitude= modelview.Longitude;
				 
				_dosyaRepo.Update(bilesen);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<List<DosyaVM>> DosyaGetir(int KayitID,int ModulID)
        {
            var liste = _dosyaRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false && a.KayitID == KayitID && a.ModulID == ModulID).ToList();
            var dosyaListesi = _mapper.Map<List<DosyaVM>>(liste);
            return dosyaListesi;
        }


        public async Task<bool> DosyaSil(int DosyaID)
        {
            var bilesen = _dosyaRepo.GetById(DosyaID);

            if (bilesen != null)
            {
                bilesen.DeleteDate = DateTime.Now;
                bilesen.IsDelete = true;
                bilesen.DeleteUserID = _userId;
                _dosyaRepo.Update(bilesen);
                await _context.SaveChangesAsync();
            }
            return true;
        }

		public async Task<bool> UploadDataToFTP(string jsonData, string fileName, string ftpUrl,string userName,string passWord)
		{
			try
			{
				ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

				// Create the full FTP URL
				var requestUrl = $"{ftpUrl}/{fileName}";

				// Create an FtpWebRequest object to handle the file upload
				var request = (FtpWebRequest)WebRequest.Create(requestUrl);
				request.Method = WebRequestMethods.Ftp.UploadFile;
				request.Credentials = new NetworkCredential(userName, passWord);
				request.EnableSsl = true;
				// Write JSON data to a MemoryStream
				using (var memoryStream = new MemoryStream())
				{
					using (var writer = new StreamWriter(memoryStream))
					{
						writer.Write(jsonData);
						writer.Flush(); // Ensure all data is written to the stream
						memoryStream.Position = 0; // Reset stream position to the beginning

						// Upload data to FTP server
						using (var ftpStream = request.GetRequestStream())
						{
							await memoryStream.CopyToAsync(ftpStream); // Asynchronously copy data to FTP stream
						}
					}
				}

				return true; // Return true if upload is successful
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				return false; // Return false if an error occurs
			}
		}

		public async Task<bool> UpdateIs360(int id, bool Is360)
		{
			var bilesen = _dosyaRepo.GetById(id);

			if (bilesen != null)
			{
				 bilesen.Is360 = Is360;
				_dosyaRepo.Update(bilesen);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<List<DosyaVM>> DosyaGetir(int ModulID)
		{
			var liste = _dosyaRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false && a.ModulID == ModulID && a.DosyaUzantisi!=".pdf").ToList();
			var dosyaListesi = _mapper.Map<List<DosyaVM>>(liste);
			return dosyaListesi;
		}

		public Task<bool> UploadDataToFTP(string jsonData, string fileName, string ftpUrl)
		{
			throw new NotImplementedException();
		}



		#endregion
	}
}
