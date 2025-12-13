using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
    [Table("Position")]
    public partial class Position : BaseEntity
    {
        public Position()
        {
            User = new HashSet<Kullanici>();
        }

        public string Name { get; set; }
        public string IdentityName { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }


        public virtual ICollection<Kullanici> User { get; set; }
    }
}
