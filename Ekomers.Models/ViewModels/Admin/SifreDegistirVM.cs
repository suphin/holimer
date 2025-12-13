using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels.Admin
{
    public class SifreDegistirVM
    {
        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string Password1 { get; set; }

        [Required(ErrorMessage = "Şifre onayı gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifreyi Onayla")]
        [Compare("Password1", ErrorMessage = "Şifreler aynı değil.")]
        public string Password2 { get; set; }
    }
}
