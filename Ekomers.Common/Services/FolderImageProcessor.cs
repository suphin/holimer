using Ekomers.Common.Services.IServices;
using Ekomers.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Services
{
    public class FolderImageProcessor : IImageProcessor
    {
        public string ProcessImage(byte[] imageBytes, string folderPath, string fileName)
        {
           // string fileName = UserService. + ".jpg";
            //string fileName = Guid.NewGuid().ToString() + ".jpg";
            string filePath = Path.Combine(folderPath, fileName + ".jpg");

            File.WriteAllBytes(filePath, imageBytes);

            return filePath;
        }
    }
}
