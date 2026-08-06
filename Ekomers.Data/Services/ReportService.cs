using Dapper; 
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace Ekomers.Data.Services
{
	public static class CryptoHelper2
	{
		private static readonly string key = "12345678901234567890123456789012";
		private static readonly string iv = "1234567890123456";

		public static string Encrypt(string plainText)
		{
			using Aes aes = Aes.Create();

			aes.Key = Encoding.UTF8.GetBytes(key);
			aes.IV = Encoding.UTF8.GetBytes(iv);

			ICryptoTransform encryptor = aes.CreateEncryptor();

			byte[] inputBuffer = Encoding.UTF8.GetBytes(plainText);

			byte[] encrypted = encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);

			return Convert.ToBase64String(encrypted);
		}

		public static string Decrypt(string cipherText)
		{
			using Aes aes = Aes.Create();

			aes.Key = Encoding.UTF8.GetBytes(key);
			aes.IV = Encoding.UTF8.GetBytes(iv);

			ICryptoTransform decryptor = aes.CreateDecryptor();

			byte[] cipherBuffer = Convert.FromBase64String(cipherText);

			byte[] decrypted = decryptor.TransformFinalBlock(cipherBuffer, 0, cipherBuffer.Length);

			return Encoding.UTF8.GetString(decrypted);
		}
	}
	public sealed class ReportService(IConfiguration config) : IReportService
	{
		private readonly string _connStr = config.GetConnectionString("DefaultConnection")!;
		private readonly string connstr = CryptoHelper2.Decrypt(config.GetConnectionString("DefaultConnection")!);
		private readonly IDictionary<string, string> _allowed =
			config.GetSection("AllowedReports").Get<Dictionary<string, string>>()
			?? new Dictionary<string, string>();


		public async Task<BankalarVM> Bankalar()
		{
			using var conn = new SqlConnection(connstr);
			await conn.OpenAsync();
			var query = "SELECT TOP 150 \r\nLGMAIN.LOGICALREF, LGMAIN.CODE, LGMAIN.DEFINITION_, LGMAIN.BRANCH, LGMAIN.ACTIVE, LGMAIN.SPECODE, LGMAIN.ORGLOGICREF\r\n FROM \r\nTIGER3ENT..LG_100_BNCARD LGMAIN WITH(NOLOCK) \r\n WHERE \r\n(LGMAIN.ACTIVE = 0)";
			var bankalar = await conn.QueryAsync<BankalarVM>(query);
			return new BankalarVM
			{
				Bankalar = bankalar.ToList()
			};
		}
		public async Task<BankaHesapVM> BankaHesaplari(int bankRef)
		{
			using var conn = new SqlConnection(connstr);
			await conn.OpenAsync();

 
			var query = @"
   SELECT
    LGMAIN.LOGICALREF,
    LGMAIN.CODE,
    LGMAIN.DEFINITION_,
    LGMAIN.ACCOUNTNO,
    ISNULL(SUM(BNTOT.DEBIT),0) AS DEBIT,
    ISNULL(SUM(BNTOT.CREDIT),0) AS CREDIT,
   ROUND(ABS(SUM(BNTOT.DEBIT)-SUM(BNTOT.CREDIT)),2) AS BAKIYE
FROM TIGER3ENT..LG_100_BANKACC LGMAIN
LEFT JOIN TIGER3ENT..LG_100_01_BNTOTFIL BNTOT
       ON LGMAIN.LOGICALREF = BNTOT.CARDREF
      AND BNTOT.TOTTYP = 1
WHERE LGMAIN.ACTIVE = 0
  AND LGMAIN.BANKREF = @BankRef
GROUP BY
    LGMAIN.LOGICALREF,
    LGMAIN.CODE,
    LGMAIN.DEFINITION_,
    LGMAIN.ACCOUNTNO
ORDER BY LGMAIN.CODE;";

			var hesaplar = await conn.QueryAsync<BankaHesapVM>(query, new
			{
				BankRef = bankRef
			});

			return new BankaHesapVM
			{
				Hesaplar = hesaplar.ToList()
			};
		}


		 
		public async Task<List<BankaHesapEkstreVM>> BankaHesapEkstresi(int cardRef)
		{
			using var conn = new SqlConnection(connstr);
			await conn.OpenAsync();

			var sql = @"SELECT
    BNTOT.YEAR_ AS Yil,
    BNTOT.MONTH_ AS AyNo,
    CASE BNTOT.MONTH_
        WHEN 0 THEN 'Devir'
        WHEN 1 THEN 'Ocak'
        WHEN 2 THEN 'Şubat'
        WHEN 3 THEN 'Mart'
        WHEN 4 THEN 'Nisan'
        WHEN 5 THEN 'Mayıs'
        WHEN 6 THEN 'Haziran'
        WHEN 7 THEN 'Temmuz'
        WHEN 8 THEN 'Ağustos'
        WHEN 9 THEN 'Eylül'
        WHEN 10 THEN 'Ekim'
        WHEN 11 THEN 'Kasım'
        WHEN 12 THEN 'Aralık'
    END AS Ay,

    ROUND(BNTOT.DEBIT,2) AS Borc,
    ROUND(BNTOT.CREDIT,2) AS Alacak,
    ROUND(BNTOT.DEBIT - BNTOT.CREDIT,2) AS Bakiye

FROM TIGER3ENT..LG_100_01_BNTOTFIL BNTOT WITH(NOLOCK)
WHERE
    BNTOT.CARDREF = @CardRef
    AND BNTOT.TOTTYP = 1
ORDER BY
    BNTOT.YEAR_,
    BNTOT.MONTH_;";

			var sonuc = await conn.QueryAsync<BankaHesapEkstreVM>(
				sql,
				new { CardRef = cardRef });

			return sonuc.ToList();
		}

		public async Task<ReportVM> RunAsync(ReportRequest request, CancellationToken ct)
		{
			if (!_allowed.TryGetValue(request.ReportKey, out var target))
				throw new InvalidOperationException("İzinli rapor listesinde yok.");

			using var conn = new SqlConnection(connstr);
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

		public async Task<List<BorcAlacakVM>> BorcAlacakRaporu()
		{
			using var conn = new SqlConnection(connstr);
			await conn.OpenAsync();

			var query = @"
					SELECT
						CHKOD,
						CHUNVAN,
						[Borç Toplamı] AS BorcToplami,
						[Alacak Toplamı] AS AlacakToplami
					FROM TIGER3ENT..VW_100_BorcAlacak
					ORDER BY CHKOD";

			var sonuc = await conn.QueryAsync<BorcAlacakVM>(query);

			return sonuc.ToList();
		}


		public async Task<List<BankaKrediVM>> BankaKredileri()
		{
			using var conn = new SqlConnection(connstr);
			await conn.OpenAsync();

			var query = @"
SELECT
    BNCREDIT.NAME_,
    BNCREDIT.CODE,
    BNCREDIT.ENDDATE,
    BNCREDIT.BEGDATE,
    BNCREDIT.TRTOTAL,
    BNCREDIT.INTTOTAL,
    BNCREDIT.BSMVTOTAL,

    ISNULL(ODEME.ANAPARA_ODENEN,0) AS ANAPARA_ODENEN,
    ISNULL(ODEME.FAIZ_ODENEN,0) AS FAIZ_ODENEN,
    ISNULL(ODEME.BSMV_ODENEN,0) AS BSMV_ODENEN,
    ISNULL(ODEME.KKDF_ODENEN,0) AS KKDF_ODENEN,

    BNCREDIT.TRTOTAL - ISNULL(ODEME.ANAPARA_ODENEN,0) AS KALAN_ANAPARA,
    BNCREDIT.INTTOTAL - ISNULL(ODEME.FAIZ_ODENEN,0) AS KALAN_FAIZ,
    BNCREDIT.BSMVTOTAL - ISNULL(ODEME.BSMV_ODENEN,0) AS KALAN_BSMV,
    BNCREDIT.KKDFTOTAL - ISNULL(ODEME.KKDF_ODENEN,0) AS KALAN_KKDF

FROM TIGER3ENT..LG_100_BNCREDITCARD BNCREDIT WITH(NOLOCK)

LEFT JOIN TIGER3ENT..LG_100_BANKACC BNACC WITH(NOLOCK)
    ON BNCREDIT.BNCRACCREF = BNACC.LOGICALREF

LEFT JOIN TIGER3ENT..LG_100_BNCARD BANKC WITH(NOLOCK)
    ON BNACC.BANKREF = BANKC.LOGICALREF

LEFT JOIN TIGER3ENT..LG_100_REPAYPLANS REPAYPLANS WITH(NOLOCK)
    ON BNCREDIT.REPAYPLANREF = REPAYPLANS.LOGICALREF

LEFT JOIN TIGER3ENT..LG_100_PROJECT PROJECT WITH(NOLOCK)
    ON BNCREDIT.PROJECTREF = PROJECT.LOGICALREF

LEFT JOIN
(
    SELECT
        CREDITREF,
        SUM(TOTAL)      AS ANAPARA_ODENEN,
        SUM(INTTOTAL)   AS FAIZ_ODENEN,
        SUM(BSMVTOTAL)  AS BSMV_ODENEN,
        SUM(KKDFTOTAL)  AS KKDF_ODENEN
    FROM TIGER3ENT..LG_100_BNCREPAYTR WITH(NOLOCK)
    WHERE TRANSTYPE = 1
    GROUP BY CREDITREF
) ODEME
    ON ODEME.CREDITREF = BNCREDIT.LOGICALREF

ORDER BY BNCREDIT.LOGICALREF";

			var sonuc = await conn.QueryAsync<BankaKrediVM>(query);

			return sonuc.ToList();
		}

		public async Task<List<KrediOdemePlaniVM>> KrediOdemePlani()
		{
			using var conn = new SqlConnection(connstr);
			await conn.OpenAsync();

			var query = @"
SELECT
    YEAR(LGMAIN.DUEDATE) AS YIL,
    MONTH(LGMAIN.DUEDATE) AS AY,

    CASE MONTH(LGMAIN.DUEDATE)
        WHEN 1 THEN 'Ocak'
        WHEN 2 THEN 'Şubat'
        WHEN 3 THEN 'Mart'
        WHEN 4 THEN 'Nisan'
        WHEN 5 THEN 'Mayıs'
        WHEN 6 THEN 'Haziran'
        WHEN 7 THEN 'Temmuz'
        WHEN 8 THEN 'Ağustos'
        WHEN 9 THEN 'Eylül'
        WHEN 10 THEN 'Ekim'
        WHEN 11 THEN 'Kasım'
        WHEN 12 THEN 'Aralık'
    END AS AYADI,

    BNCREDIT.NAME_ AS KREDIADI,

    SUM(
        LGMAIN.TOTAL +
        LGMAIN.INTTOTAL +
        LGMAIN.BSMVTOTAL +
        LGMAIN.KKDFTOTAL
    ) AS TUTAR
,LGMAIN.DUEDATE
FROM TIGER3ENT..LG_100_BNCREPAYTR LGMAIN WITH(NOLOCK)

INNER JOIN TIGER3ENT..LG_100_BNCREDITCARD BNCREDIT WITH(NOLOCK)
    ON LGMAIN.CREDITREF = BNCREDIT.LOGICALREF

WHERE
    LGMAIN.TRANSTYPE = 0
    AND LGMAIN.BNCRPARENTREF = 0

    AND NOT EXISTS
    (
        SELECT 1
        FROM TIGER3ENT..LG_100_BNCREPAYTR ODEME WITH(NOLOCK)
        WHERE ODEME.TRANSTYPE = 1
        AND ODEME.PARENTREF = LGMAIN.LOGICALREF
    )

GROUP BY

YEAR(LGMAIN.DUEDATE),
MONTH(LGMAIN.DUEDATE),
BNCREDIT.NAME_,LGMAIN.DUEDATE

ORDER BY

YEAR(LGMAIN.DUEDATE),
MONTH(LGMAIN.DUEDATE),
BNCREDIT.NAME_;";

			var sonuc = await conn.QueryAsync<KrediOdemePlaniVM>(query);

			return sonuc.ToList();
		}
	}
}
