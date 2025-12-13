using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ekomers.Models;

namespace Ekomers.Data.Services.IServices
{
	public interface IReportService
	{
		Task<ReportVM> RunAsync(ReportRequest request, CancellationToken ct);
	}
}
