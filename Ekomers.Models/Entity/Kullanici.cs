using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 

namespace Ekomers.Models.Ekomers
{
    public class Kullanici : IdentityUser
    {
        [Required] 
        public string AdSoyad { get; set; }
        public DateTime SonGirisTarihi { get; set; }
        public string? Unvan { get; set; }   
        public string? Departman { get; set; }   
        public int ImageID { get; set; }
        public bool IsActive { get; set; }
        public int? DepartmanID { get; set; }
        public int? SirketID { get; set; }
        public int? BirimID { get; set; }

        public string? Bolum { get; set; }
		public bool IsCrmUser{ get; set; }=false;
		public bool IsMhUser{ get; set; }=false;
		public bool IsEticaretUser{ get; set; }=false;
	}
 
    
}
