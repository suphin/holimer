using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class DosyaYonetimiVM
    {
        public int ModulID { get; set; }
        public int KayitID { get; set; }
        public string DosyaYolu { get; set; }
        public string ControllerName { get; set; }
        public IFormFile? Dosya { get; set; }
        public List<DosyaVM>? DosyaListe { get; set; }
    }
}
