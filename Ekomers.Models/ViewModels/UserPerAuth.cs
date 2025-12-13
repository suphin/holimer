using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class UserPerAuth : BaseVM
    {
        public int UserID { get; set; }
        public int FormID { get; set; }
        public string FormName { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Remove { get; set; }
        public bool Confirm { get; set; }
        public int MenuID { get; set; }
        public int SubCategoryID { get; set; }
        public bool IsShortCut { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public string Module { get; set; }
        public string CategoryName { get; set; }

    }
}
