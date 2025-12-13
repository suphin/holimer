using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ekomers.Models.ViewModels
{
    public class SignInVM
    {
        public SignInVM() { }

        public SignInVM(string email, string password)
        {
            Email = email;
            Password = password;
        }

        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "E-Posta adresi gereklidir.")]
        [Display(Name = "E-Posta Adresiniz")]
        [EmailAddress(ErrorMessage = "ePosta adresiniz doğru formatta değil")]
        public string Email { get; set; }


        [Required(ErrorMessage = "Şifreniz gereklidir.")]
        [Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter uzunluğunda olmalıdır.")]
        public string Password { get; set; }
    }
}
