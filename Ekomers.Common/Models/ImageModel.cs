using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Models
{
    public class ImageModel
    {
         
        public int ImageId { get; set; }

         
        public byte[] ImageData { get; set; }
    }
}
