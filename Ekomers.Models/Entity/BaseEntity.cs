using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
    public class BaseEntity
    {
        public int ID { get; set; }
        [Display(Name = "Aktif Mi?")]
        public bool? IsActive { get; set; }
        [Display(Name = "Silindi Mi?")]
        public bool? IsDelete { get; set; }
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime? CreateDate { get; set; }
        [Display(Name = "Silinme Tarihi")]
        public DateTime? DeleteDate { get; set; }
        public string? CreateUserID { get; set; }
        public string? DeleteUserID { get; set; }
        public string? DosyaID { get; set; }		 
		public DateTime? UpdateDate { get; set; } 
		public string? UpdateUserID { get; set; }
	}
}
