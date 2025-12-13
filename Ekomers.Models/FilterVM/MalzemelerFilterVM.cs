using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.FilterVM
{
	public sealed class MalzemelerFilterVM
	{
		public int? GrupID { get; set; }
		public int? AltGrupID { get; set; }
		public string? Aciklama { get; set; }
		public bool SadeceAktif { get; set; } = true;

		// Sayfalama linklerine kolayca enjekte etmek için:
		public Dictionary<string, string?> ToRouteValues()
		{
			var d = new Dictionary<string, string?>();
			if (GrupID.HasValue) d["GrupID"] = GrupID.Value.ToString();
			if (AltGrupID.HasValue) d["AltGrupID"] = AltGrupID.Value.ToString();
			if (!string.IsNullOrWhiteSpace(Aciklama)) d["Aciklama"] = Aciklama;
			// Her zaman yaz: true/false
			d["SadeceAktif"] = SadeceAktif ? "true" : "false";
			return d;
		}
	}

}
