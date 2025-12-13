using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels.Admin
{
    
    public class AddRoleClaimVM
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        public List<Claim> ExistingClaims { get; set; } = new List<Claim>();
		public IEnumerable<Claim> ExistingClaims2 { get; set; } = Enumerable.Empty<Claim>();
		public List<YetkilendirmeVM> Yetkiler { get; set; }
	}
}
