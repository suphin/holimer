using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Models
{
	public class MalzemeFiyatGuncelleDto
	{
		public int MalzemeId { get; set; }
		public double? YeniFiyat { get; set; }
		public double? YeniMaliyet { get; set; }
		public double? YeniKdv { get; set; }
		public int? DovizTur { get; set; }
		public string? Aciklama { get; set; }
	}

}
