using Ekomers.Models.Ekomers;
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class OrtakListeVM
    {
        
        public List<Departman>? IlgiliMudurlukListe { get; set; }
       
        public List<DosyaVM>? DosyaListe { get; set; } 
         
    }
}
