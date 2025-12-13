using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public sealed class ReportRequest
	{
		// appsettings:AllowedReports'taki anahtar (kullanıcıdan gelen)
		public string ReportKey { get; set; } = "";
		// İsteğe bağlı parametreler (SP parametreleri vs.)
		public Dictionary<string, string?> Parameters { get; set; } = new();
		// Basit sayfalama (opsiyonel)
		public int PageIndex { get; set; } = 1;   // 1-based
		public int PageSize { get; set; } = 50;
		// NEW: export tüm kayıtlar mı?
		public bool ExportAll { get; set; }
		public  DateTime StartDate { get; set; }
		public  DateTime EndDate { get; set; }
	}
}
