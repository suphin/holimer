using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public sealed class PagerModel
	{
		public required int TotalCount { get; init; }
		public required int PageIndex { get; init; } // 1-based
		public required int PageSize { get; init; }
		public int MaxPagesToShow { get; init; } = 7;

		public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
	}
}
