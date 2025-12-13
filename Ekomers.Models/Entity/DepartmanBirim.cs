using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
    public class DepartmanBirim:BaseEntity
    {
        public int? DepartmanID { get; set; }
        [ForeignKey("DepartmanID")]
        public virtual Departman? Departman { get; set; }
        public string Ad { get; set; }
        public string Aciklama { get; set; }
    }
}
