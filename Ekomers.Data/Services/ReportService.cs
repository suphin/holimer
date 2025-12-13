using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Ekomers.Data.Services
{
	public sealed class ReportService(IConfiguration config) : IReportService
	{
		private readonly string _connStr = config.GetConnectionString("DefaultConnection")!;
		private readonly IDictionary<string, string> _allowed =
			config.GetSection("AllowedReports").Get<Dictionary<string, string>>()
			?? new Dictionary<string, string>();

		public async Task<ReportVM> RunAsync(ReportRequest request, CancellationToken ct)
		{
			if (!_allowed.TryGetValue(request.ReportKey, out var target))
				throw new InvalidOperationException("İzinli rapor listesinde yok.");

			using var conn = new SqlConnection(_connStr);
			await conn.OpenAsync(ct);

			// Parametre bağlama
			var dyn = new DynamicParameters();
			foreach (var kv in request.Parameters)
				dyn.Add(kv.Key, kv.Value);
			dyn.Add("ExportAll", request.ExportAll);
			// Basit sayfalama desteği (opsiyonel): 
			// Eğer SP sayfalama döndürmüyorsa, burada sadece DataTable'ı kırpmak yerine 
			// SP içinde OFFSET/FETCH kullanman önerilir.
			dyn.Add("PageIndex", request.PageIndex);
			dyn.Add("PageSize", request.PageSize);

			



			// SP mi, View mü? Basit sezgi: "rpt_" ile başlıyorsa SP; yoksa View kabul edelim.
			var isStoredProc = target.StartsWith("rpt_", StringComparison.OrdinalIgnoreCase);

			using var cmd = conn.CreateCommand();
			cmd.CommandText = target;
			cmd.CommandType = isStoredProc ? CommandType.StoredProcedure : CommandType.Text;

			if (isStoredProc)
			{
				foreach (var pName in dyn.ParameterNames)
				{
					var p = cmd.CreateParameter();
					p.ParameterName = pName;
					p.Value = dyn.Get<object?>(pName) ?? DBNull.Value;
					cmd.Parameters.Add(p);
				}
			}
			if (!isStoredProc)
				cmd.CommandText = $"SELECT * FROM {target}";

			using var reader = await cmd.ExecuteReaderAsync(ct);

			// ---- 1. result set -> DataTable
			var schema = reader.GetSchemaTable();
			var table = new DataTable();
			foreach (DataRow r in schema.Rows)
			{
				var colName = (string)r["ColumnName"];
				var dataType = (Type)r["DataType"];
				table.Columns.Add(colName, dataType);
			}
			while (await reader.ReadAsync(ct))
			{
				var values = new object[reader.FieldCount];
				reader.GetValues(values);
				table.Rows.Add(values);
			}

			int total = table.Rows.Count;

			// ---- 2. result set -> TotalCount
			if (await reader.NextResultAsync(ct) && reader.FieldCount > 0)
			{
				if (reader.GetName(0).Equals("TotalCount", StringComparison.OrdinalIgnoreCase)
					&& await reader.ReadAsync(ct))
				{
					total = Convert.ToInt32(reader.GetValue(0));
				}
			}
			return new ReportVM
			{
				Title = request.ReportKey,
				Table = table,
				TotalCount = total,
				PageIndex = request.PageIndex,
				PageSize = request.PageSize,
				ReportKey = request.ReportKey,
				Parameters = request.Parameters
			};
		}
	}
}
