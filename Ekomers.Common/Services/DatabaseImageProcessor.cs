
using Ekomers.Common.Models;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Services
{
     
    public class DatabaseImageProcessor : IImageProcessor
    {
        private readonly ApplicationDbContext _context;
        public DatabaseImageProcessor(ApplicationDbContext context)
        {
            _context = context;
        }
        public string ProcessImage(byte[] imageBytes, string folderPath, string fileName)
        {
            // Bu kısmı, uygulamanızın gerçek veritabanı modeline göre düzenleyin

            UserImage imageModel = new UserImage
            {
                    ImageData = imageBytes
                };

                _context.UserImage.Add(imageModel);
                _context.SaveChanges();
            

            return "Veritabanına kaydedildi.";
        }
    }
}
