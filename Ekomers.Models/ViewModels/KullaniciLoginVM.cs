using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ekomers.Models.ViewModels
{
    public class KullaniciLoginVM
    {
        [Display(Name = "Kullanıcı Adı")]
        [Required(ErrorMessage = "Kullanıcı Adı girilmesi zorunludur")]
        public string UserName { get; set; }

        [Display(Name = "Şifre")]
        [Required(ErrorMessage = "Şifre girilmesi zorunludur")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "şifreniz en az 6 karakter olmalıdır.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
