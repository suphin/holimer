using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels.Yardim
{
    
    public class MahalleVM : BaseEntity
    {
        public string Ad { get; set; }
        public string Aciklama { get; set; }
        public int YardimMiktari { get; set; }
        public string? CreateUserName { get; set; }
        public int? CreateUserID { get; set; }
        public string? DeleteUserName { get; set; }
        public int? DeleteUserID { get; set; }
    }
}
