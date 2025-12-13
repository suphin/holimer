using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels.Admin
{
    public class RoleVM
    {
        [Display(Name = "Rol İsmi")]
        [Required(ErrorMessage = "Rol ismi zorunludur.")]
        public string Name { get; set; }

        public string ID { get; set; }
    }
}
