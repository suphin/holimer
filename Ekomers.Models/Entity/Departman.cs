using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
    public class Departman:BaseEntity
    {
        public Departman()
        {
            Birimler = new HashSet<DepartmanBirim>();
        }
        public string Ad { get; set; }
        public string Aciklama { get; set; }

        public ICollection<DepartmanBirim>? Birimler { get; set; }
    }
}
