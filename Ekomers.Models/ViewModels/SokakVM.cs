using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class SokakVM:BaseVM
    {
        public string Ad { get; set; }
        public string Aciklama { get; set; }
        public int MahalleID { get; set; }

        public Mahalle Mahalle { get; set; }
    }

}
