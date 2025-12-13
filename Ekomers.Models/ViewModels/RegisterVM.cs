using Ekomers.Models.Ekomers;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels
{
    public class RegisterVM
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
		//[RegularExpression(@"^0[5]\d{9}$", ErrorMessage = "Telefon numarası 0532 123 12 12 formatında olmalıdır.")]
		//[RegularExpression(@"^0[5]\d{2} \d{3} \d{2} \d{2}$", ErrorMessage = "Telefon numarası 0532 123 12 12 formatında olmalıdır.")]
		public string? PhoneNumber { get; set; }


        [Required(ErrorMessage = "Şifreniz gereklidir.")]
        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 8 karakter uzunluğunda olmalıdır. Büyük küçük harf, sayı ve özel karakter barındırmalıdır.")]
        public string? Password { get; set; }


		[Required(ErrorMessage = "Şifreniz gereklidir.")]
		[Display(Name = "Şifre")]
		[DataType(DataType.Password)]
		[MinLength(6, ErrorMessage = "Şifreniz en az 8 karakter uzunluğunda olmalıdır. Büyük küçük harf, sayı ve özel karakter barındırmalıdır.")]
		public string? ConfirmPassword { get; set; }


		[Range(1, int.MaxValue, ErrorMessage = "Departman seçili olmalıdır.")]
        [Display(Name = "Departman")]
        public int? DepartmanID { get; set; }


		[Range(1, int.MaxValue, ErrorMessage = "Şirket seçili olmalıdır.")]
		[Display(Name = "Şirket")]
		public int? SirketID { get; set; }
	}
}
