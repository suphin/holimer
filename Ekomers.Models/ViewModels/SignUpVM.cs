using Ekomers.Models.Ekomers;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels
{
    public class SignUpVM
    {
        [Required(ErrorMessage = "Ad Soyad gereklidir.")]
        [Display(Name = "Ad Soyad")]
        public string? AdSoyad { get; set; }


        [Required(ErrorMessage = "Kullanıcı ismi gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string? UserName { get; set; }


        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "E-Posta adresi gereklidir.")]
        [Display(Name = "ePosta Adresiniz")]
        [EmailAddress(ErrorMessage = "E-Posta adresiniz doğru formatta değil")]
        public string? Email { get; set; }


        [Display(Name = "Telefon Numarası")]
        [DataType(DataType.PhoneNumber)]
        [Required(ErrorMessage = "Telefon Numarası gereklidir.")]
		[RegularExpression(@"^0[5]\d{9}$", ErrorMessage = "Telefon numarası 0532 123 12 12 formatında olmalıdır.")]
		public string? PhoneNumber { get; set; }


        [Required(ErrorMessage = "Şifreniz gereklidir.")]
        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter uzunluğunda olmalıdır.")]
        public string? Password { get; set; }
        public string? Departman { get; set; }
        public string? Bolum { get; set; }
        public string? Unvan { get; set; }
        public int? DepartmanID { get; set; }
		[Display(Name = "Şirket")]
		public int? SirketID { get; set; }

        public List<Departman>? DepartmanListe { get; set; }

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
    }
}
