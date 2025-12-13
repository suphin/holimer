using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels.Admin
{
    
    public class AddUserClaimVM
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ClaimType { get; set; }  
        public string ClaimValue { get; set; }
        public IList<Claim> ExistingClaims { get; set; } = new List<Claim>();
        public IEnumerable<IdentityRole> AllRoles { get; set; } = new List<IdentityRole>();
       

        public IList<string> UserRoles { get; set; } = new List<string>();
        public List<YetkilendirmeVM> Yetkiler { get; set; } 
        
    }
}
