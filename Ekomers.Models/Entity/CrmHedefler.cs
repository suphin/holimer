using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class CrmHedefler:BaseEntity
	{
		public string UserID { get; set; }
		public int Yil { get; set; }
		public int Ay { get; set; }
		public int TurID { get; set; }
		public double Miktar { get; set; }
		public double Tutar { get; set; }
	}
}
public class HedefCellVM
{
	public int Ay { get; set; }
	public double? Miktar { get; set; }
	public double? Tutar { get; set; }
}

public class KullaniciHedefVM
{
	public string UserID { get; set; }
	public string UserName { get; set; }
	public List<HedefCellVM> Hedefler { get; set; } = new();
}

public class HedefTabloVM
{
	public int Yil { get; set; }
	public List<KullaniciHedefVM> KullaniciHedefleri { get; set; } = new();
}