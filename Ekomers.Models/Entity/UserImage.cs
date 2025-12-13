using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
     
    public class UserImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        [Column(TypeName = "varbinary(MAX)")]
        public byte[] ImageData { get; set; }
    }
}
