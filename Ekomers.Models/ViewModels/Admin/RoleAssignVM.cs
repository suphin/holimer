using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels.Admin
{
    public class RoleAssignVM
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool Checked { get; set; }
    }
}
