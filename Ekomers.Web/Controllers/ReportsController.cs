using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Admin,Rapor")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	 
	public  class ReportsController : BaseController
	{
		private readonly IReportService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		public ReportsController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IReportService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
		}
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
		[HttpGet]
		public IActionResult Index()
		{
			ViewBag.Modul = "Rapor";
			return View(); // filtre formu
		}

		[HttpPost]
		public async Task<IActionResult> Run(ReportRequest request, CancellationToken ct)
		{
			// Basit param örneği: boş değerleri atabilirsiniz
			request.Parameters = request.Parameters
				.Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
				.ToDictionary(kv => kv.Key, kv => kv.Value);

			var vm = await _service.RunAsync(request, ct);
			return View("Result", vm);
		}

		[HttpPost]
		public async Task<IActionResult> ExportCsv(ReportRequest request, CancellationToken ct)
		{
			var vm = await _service.RunAsync(request, ct);
			if (vm.Table is null) return NoContent();

			using var sw = new StringWriter();
			// header
			sw.WriteLine(string.Join(",", vm.Table.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"")));
			// rows
			foreach (DataRow r in vm.Table.Rows)
			{
				var cells = vm.Table.Columns.Cast<DataColumn>()
					.Select(c => r[c] == DBNull.Value ? "" : r[c]?.ToString()?.Replace("\"", "\"\""));
				sw.WriteLine(string.Join(",", cells.Select(x => $"\"{x}\"")));
			}
			var bytes = System.Text.Encoding.UTF8.GetBytes(sw.ToString());
			return File(bytes, "text/csv", $"{request.ReportKey}_{DateTime.Now:yyyyMMddHHmmss}.csv");
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExportXlsx(ReportRequest request, CancellationToken ct)
		{
			// İstersen tüm kayıtları export et:
			// request.PageIndex = 1;
			// request.PageSize  = int.MaxValue;
			// Tüm veriyi istiyorsa PageSize’ı büyüt veya SP’ye ExportAll=1 gönder
			if (request.ExportAll)
			{
				request.PageIndex = 1;
				request.PageSize = int.MaxValue; // SP tarafında @ExportAll bakıyoruz zaten
			}

			var vm = await _service.RunAsync(request, ct);

			if (vm.Table is null || vm.Table.Rows.Count == 0) return NoContent();
			//var vm = await _service.RunAsync(request, ct);
			if (vm.Table is null || vm.Table.Rows.Count == 0)
				return NoContent();

			using var wb = new ClosedXML.Excel.XLWorkbook();
			var ws = wb.Worksheets.Add(string.IsNullOrWhiteSpace(vm.Title) ? "Report" : vm.Title);

			var table = vm.Table;

			// 1) Başlıklar
			for (int c = 0; c < table.Columns.Count; c++)
				ws.Cell(1, c + 1).Value = table.Columns[c].ColumnName;

			// 2) Satırlar
			for (int r = 0; r < table.Rows.Count; r++)
			{
				for (int c = 0; c < table.Columns.Count; c++)
				{
					var val = table.Rows[r][c];
					var cell = ws.Cell(r + 2, c + 1);

					if (val == DBNull.Value)
					{
						cell.Clear(); // boş
						continue;
					}

					// Basit tip biçimlendirme
					switch (val)
					{
						case DateTime dt:
							cell.Value = dt;
							cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
							break;
						case decimal dec:
							cell.Value = dec;
							cell.Style.NumberFormat.Format = "#,##0.00";
							break;
						case double d:
							cell.Value = d;
							cell.Style.NumberFormat.Format = "#,##0.00";
							break;
						case float f:
							cell.Value = f;
							cell.Style.NumberFormat.Format = "#,##0.00";
							break;
						case int or long or short or byte:
							cell.Value = (ClosedXML.Excel.XLCellValue)val;
							cell.Style.NumberFormat.Format = "0";
							break;
						default:
							cell.Value = val?.ToString();
							break;
					}
				}
			}

			// 3) Tablo görünümü + otomatik filtre ve kolon genişliği
			var lastRow = table.Rows.Count + 1;
			var lastCol = table.Columns.Count;
			var rng = ws.Range(1, 1, lastRow, lastCol);
			rng.CreateTable();                 // header’ı otomatik tanır
			ws.Columns(1, lastCol).AdjustToContents();

			// 4) Dosyayı memory stream’e yaz ve gönder
			using var ms = new MemoryStream();
			wb.SaveAs(ms);
			var bytes = ms.ToArray();

			var fileName = $"{vm.Title}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
			const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
			return File(bytes, contentType, fileName);
		}

	}
}
