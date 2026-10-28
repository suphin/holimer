using System.Globalization;
using Dapper;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

public sealed partial class LogoPurchaseInvoiceController
{
    private async Task<List<LogoPurchaseInvoiceCompanyVM>> CompaniesAsync(CancellationToken ct)
    {
        var configured=await _portalCompanies.ListAsync(ct);
        if(configured.Count==0)return [];
        var metadata=(await _logoContext.Database.GetDbConnection().QueryAsync<LogoPurchaseInvoiceCompanyVM>(new CommandDefinition("""
            SELECT f.NR AS Number,v.name AS ViewName
            FROM dbo.L_CAPIFIRM f
            OUTER APPLY(SELECT TOP(1) v.name FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id
                WHERE s.name=N'dbo' AND LOWER(v.name)=N'fatura_dokumu_'+RIGHT('000'+CONVERT(varchar(3),f.NR),3)
                ORDER BY v.object_id) v
            WHERE f.NR IN @numbers AND f.NR BETWEEN 1 AND 999 ORDER BY f.NR
            """,new{numbers=configured.Select(x=>x.Number).ToArray()},commandTimeout:20,cancellationToken:ct))).ToDictionary(x=>x.Number);
        return configured.Where(x=>metadata.ContainsKey(x.Number)).Select(x=>new LogoPurchaseInvoiceCompanyVM{Number=x.Number,Name=x.Name,ViewName=metadata[x.Number].ViewName}).ToList();
    }

    private async Task<PurchaseSource> ResolveSourceAsync(int firm, DateTime start, DateTime end, CancellationToken ct)
    {
        if (firm is < 1 or > 999) throw new ArgumentException("Geçerli bir Logo firması seçiniz.");
        if(start.Year<1753 || end.Year>9998 || end.Date<start.Date) throw new ArgumentException("Geçerli bir tarih aralığı seçiniz (1753–9998).");
        var company = (await CompaniesAsync(ct)).SingleOrDefault(c => c.Number == firm)
            ?? throw new ArgumentException("Seçilen firma portal Şirketler listesinde tanımlı veya aktif değil ya da Logo'da bulunamadı.");
        if (!company.HasInvoiceView) throw new ArgumentException($"{firm:000} firması için dbo.fatura_dokumu_{firm:000} view'ı bulunamadı. Başka firmadan veri okunmadı.");
        var number = firm.ToString("000", CultureInfo.InvariantCulture);
        var periods = (await _logoContext.Database.GetDbConnection().QueryAsync<PurchasePeriod>(new CommandDefinition("""
            SELECT NR AS Number,BEGDATE AS BeginDate,ENDDATE AS EndDate FROM dbo.L_CAPIPERIOD
            WHERE FIRMNR=@firm AND NR BETWEEN 1 AND 99 AND BEGDATE<@endExclusive AND ENDDATE>=@start ORDER BY NR
            """,new {firm,start=start.Date,endExclusive=end.Date.AddDays(1)},commandTimeout:20,cancellationToken:ct))).ToList();
        if(periods.Count==0) throw new ArgumentException("Seçilen firma ve tarih aralığı için Logo dönemi bulunamadı.");
        static string Quote(string name)=>"["+name.Replace("]","]]")+"]";
        // Firm and period identifiers come only from bounded ids validated against Logo metadata.
        // The invoice view remains the source of business filters and line amounts.
        var sources = periods.Select(p => $"""
            SELECT CONVERT(nvarchar(64),i.[GUID]) AS [GUID],i.EINVOICE,i.ESTATUS,i.FICHENO,i.DATE_,i.LOGICALREF,c.CODE AS SupplierCode
            FROM [dbo].[LG_{number}_{p.Number.ToString("00",CultureInfo.InvariantCulture)}_INVOICE] i
            JOIN [dbo].[LG_{number}_CLCARD] c ON c.LOGICALREF=i.CLIENTREF
            WHERE i.CANCELLED=0 AND i.DATE_>=@startDate AND i.DATE_<@endExclusive
              AND i.DATE_>=CONVERT(datetime2,'{p.BeginDate:yyyy-MM-dd}',23)
              AND i.DATE_<DATEADD(day,1,CONVERT(datetime2,'{p.EndDate:yyyy-MM-dd}',23))
            """);
        return new(company.Name,"[dbo]."+Quote(company.ViewName!),string.Join(" UNION ALL ",sources));
    }
    private sealed record PurchaseSource(string CompanyName,string View,string InvoiceSource);
    private sealed class PurchasePeriod
    {
        public int Number { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
