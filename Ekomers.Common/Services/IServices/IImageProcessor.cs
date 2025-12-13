using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Services.IServices
{
    public interface IImageProcessor
    {
        string ProcessImage(byte[] imageBytes, string folderPath,string fileName);
    }
}
