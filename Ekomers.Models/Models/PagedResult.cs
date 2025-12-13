using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public sealed class PagedResult<T>
	{
		public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
		public int PageIndex { get; init; }           // 1-based
		public int PageSize { get; init; }
		public int TotalCount { get; init; }
		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
		public bool HasPreviousPage => PageIndex > 1;
		public bool HasNextPage => PageIndex < TotalPages;

	}

}
