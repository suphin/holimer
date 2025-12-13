using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Yetkilendirme:BaseEntity
	{
		public string Ad { get; set; }
		public string Aciklama { get; set; }
		public int KategoriID { get; set; }

		public string PolicyName { get; set; }
		public string ClaimType { get; set; }
		public string ClaimName { get; set; }

	}
}
