using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{ 
	public sealed class ReportVM
	{
		public string Title { get; set; } = "";
		public System.Data.DataTable? Table { get; set; }

		public int TotalCount { get; set; }
		public int PageIndex { get; set; }
		public int PageSize { get; set; }

		// yeniden post için:
		public string ReportKey { get; set; } = "";
		public Dictionary<string, string?> Parameters { get; set; } = new();

		public IReadOnlyList<string> Columns =>
			Table is null ? [] : Table.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList();
	}
}
