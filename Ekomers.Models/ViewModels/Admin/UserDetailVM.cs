using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Admin
{
    public class UserDetailVM
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad gereklidir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; }


        [Required(ErrorMessage = "Kullanıcı ismi gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string UserName { get; set; }


        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "ePosta adresi gereklidir.")]
        [Display(Name = "ePosta Adresiniz")]
        [EmailAddress(ErrorMessage = "ePosta adresiniz doğru formatta değil")]
        public string Email { get; set; }


        [Display(Name = "Telefon Numarası")]
        [DataType(DataType.PhoneNumber)]
        [Required(ErrorMessage = "Telefon Numarası gereklidir.")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Departman")] 
        [Required(ErrorMessage = "Departman gereklidir.")]
        public string Departman { get; set; }


        [Display(Name = "DepartmanID")] 
        [Required(ErrorMessage = "DepartmanID gereklidir.")]
        public int? DepartmanID { get; set; }


        [Display(Name = "Ünvan")] 
        [Required(ErrorMessage = "Ünvan gereklidir.")]
        public string Unvan { get; set; }
        public string? Bolum { get; set; }

        [Required(ErrorMessage = "Şifreniz gereklidir.")]
        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter uzunluğunda olmalıdır.")]
        public string? Password1 { get; set; }

        [Required(ErrorMessage = "Şifre Tekrar gereklidir.")]
        [Display(Name = "Şifre Tekrar")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter uzunluğunda olmalıdır.")]
        public string? Password2 { get; set; }
        [Display(Name ="Crm Kullanıcısı mı?")]
        public bool IsCrmUser { get; set; } = false;
		[Display(Name = "Müşteri Hizmetleri Kullanıcısı mı?")]
		public bool IsMhUser { get; set; } = false;
        [Display(Name = "E-Ticaret Kullanıcısı mı?")]
		public bool IsEticaretUser { get; set; } = false;

		[Range(1, int.MaxValue, ErrorMessage = "Şirket seçili olmalıdır.")]
		[Display(Name = "Şirket")]
		public int? SirketID { get; set; }
	}
}
