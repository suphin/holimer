using Dapper;
using Ekomers.Models.ViewModels.Raporlar;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services.Reports;

public interface ILogoCustomerStatementService
{
    Task<List<LogoCariFirmaVM>> CompaniesAsync(CancellationToken ct);
    Task<List<LogoCariDonemVM>> PeriodsAsync(int firm, CancellationToken ct);
    Task<LogoCariScopeVM> ScopeAsync(int firm, int period, DateTime? start, DateTime? end, CancellationToken ct);
    Task<LogoCariRaporVM> AccountsAsync(LogoCariScopeVM scope, string search, bool includePassive, bool onlyBalance, int page, CancellationToken ct);
    Task<LogoCariEkstreVM> StatementAsync(LogoCariScopeVM scope, int client, int page, CancellationToken ct);
    Task<LogoCariBelgeVM> DocumentAsync(LogoCariScopeVM scope, int client, int movement, int page, CancellationToken ct);
}

/// <summary>SELECT-only Logo report. Identifiers are built only from bounded numeric firm/period ids.</summary>
public sealed partial class LogoCustomerStatementService(LogoContext logo,PortalLogoCompanyService portalCompanies) : ILogoCustomerStatementService
{
    private readonly System.Data.Common.DbConnection connection = logo.Database.GetDbConnection();
    private CommandDefinition Command(string sql, object? args, CancellationToken ct) => new(sql, args, commandTimeout: 60, cancellationToken: ct);
    private static void ValidateFirm(int firm) { if (firm is < 1 or > 999) throw new ArgumentException("Geçerli Logo firması seçiniz."); }
    private static void ValidatePeriod(int period) { if (period is < 1 or > 99) throw new ArgumentException("Geçerli Logo dönemi seçiniz."); }
    private static void ValidatePage(int page) { if (page is < 1 or > 1000000) throw new ArgumentException("Sayfa numarası geçersiz."); }
    private static string FirmTable(int firm, string suffix) { ValidateFirm(firm); return $"dbo.[LG_{firm:000}_{suffix}]"; }
    private static string Table(LogoCariScopeVM s, string suffix) { ValidatePeriod(s.Period.Number); return FirmTable(s.Company.Number, $"{s.Period.Number:00}_{suffix}"); }

    public async Task<List<LogoCariFirmaVM>> CompaniesAsync(CancellationToken ct)
    {
        var configured=await portalCompanies.ListAsync(ct);
        if(configured.Count==0)return [];
        var metadata=(await connection.QueryAsync<LogoCariFirmaVM>(Command("SELECT NR AS Number,COALESCE(LOCALCTYP,0) AS LocalCurrency FROM dbo.L_CAPIFIRM WHERE NR IN @numbers AND NR BETWEEN 1 AND 999 ORDER BY NR",new{numbers=configured.Select(x=>x.Number).ToArray()},ct))).ToDictionary(x=>x.Number);
        return configured.Where(x=>metadata.ContainsKey(x.Number)).Select(x=>new LogoCariFirmaVM{Number=x.Number,Name=x.Name,LocalCurrency=metadata[x.Number].LocalCurrency}).ToList();
    }
    public async Task<List<LogoCariDonemVM>> PeriodsAsync(int firm, CancellationToken ct)
    {
        ValidateFirm(firm);
        return (await connection.QueryAsync<LogoCariDonemVM>(Command("SELECT NR AS Number,CONVERT(date,BEGDATE) AS BeginDate,CONVERT(date,ENDDATE) AS EndDate FROM dbo.L_CAPIPERIOD WHERE FIRMNR=@firm AND NR BETWEEN 1 AND 99 ORDER BY NR DESC", new { firm }, ct))).ToList();
    }
    public async Task<LogoCariScopeVM> ScopeAsync(int firm, int period, DateTime? start, DateTime? end, CancellationToken ct)
    {
        ValidateFirm(firm);
        var company = (await CompaniesAsync(ct)).SingleOrDefault(x => x.Number == firm) ?? throw new KeyNotFoundException("Seçilen firma portal Şirketler listesinde tanımlı veya aktif değil ya da Logo'da bulunamadı.");
        var periods = await PeriodsAsync(firm, ct);
        var chosen = period == 0 ? periods.FirstOrDefault() : periods.SingleOrDefault(x => x.Number == period);
        if (chosen == null) throw new KeyNotFoundException("Seçilen firma için Logo dönemi bulunamadı.");
        var from = (start ?? new DateTime(Math.Clamp(DateTime.Today.Year, chosen.BeginDate.Year, chosen.EndDate.Year), 1, 1)).Date;
        if (!start.HasValue && from < chosen.BeginDate) from = chosen.BeginDate;
        var until = (end ?? (DateTime.Today < chosen.BeginDate ? chosen.BeginDate : DateTime.Today > chosen.EndDate ? chosen.EndDate : DateTime.Today)).Date;
        if (from < chosen.BeginDate || until > chosen.EndDate || until >= DateTime.MaxValue.Date || from < new DateTime(1900,1,1) || until < from)
            throw new ArgumentException("Tarih aralığı seçilen Logo döneminin içinde olmalı; bitiş başlangıçtan önce olamaz.");
        return new() { Company = company, Period = chosen, StartDate = from, EndDate = until };
    }
    private static string MovementSource(LogoCariScopeVM s) => $"""
        SELECT L.LOGICALREF AS MovementRef,L.CLIENTREF AS ClientRef,L.SOURCEFREF AS SourceRef,
               L.MODULENR AS Module,L.TRCODE AS TransactionCode,L.DATE_ AS [Date],COALESCE(L.FTIME,0) AS [Time],
               COALESCE(L.TRANNO,N'') AS Number,COALESCE(L.DOCODE,N'') AS DocumentNumber,COALESCE(L.LINEEXP,N'') AS Description,
               CAST(CASE WHEN L.SIGN=0 THEN COALESCE(L.AMOUNT,0) ELSE 0 END AS decimal(28,6)) AS Debit,
               CAST(CASE WHEN L.SIGN=1 THEN COALESCE(L.AMOUNT,0) ELSE 0 END AS decimal(28,6)) AS Credit,
               COALESCE(L.TRCURR,0) AS CurrencyType,CAST(CASE WHEN COALESCE(L.TRCURR,0) IN (0,160) THEN COALESCE(L.AMOUNT,0) ELSE COALESCE(L.TRNET,0) END AS decimal(28,6)) AS CurrencyAmount,
               CAST(COALESCE(L.TRRATE,0) AS decimal(28,8)) AS ExchangeRate
        FROM {Table(s,"CLFLINE")} L
        WHERE COALESCE(L.CANCELLED,0)=0 AND L.SIGN IN (0,1) AND L.DATE_>=@periodBegin AND L.DATE_<@endExclusive
        """;
    private static object Args(LogoCariScopeVM s, int page, int client = 0, int movement = 0) => new
    { periodBegin = s.Period.BeginDate, start = s.StartDate, endExclusive = s.EndDate.AddDays(1), skip = (long)(page-1)*50, client, movement };
    private static string ClientFields => "C.LOGICALREF AS ClientRef,COALESCE(C.CODE,N'') AS Code,COALESCE(C.DEFINITION_,N'') AS Name,COALESCE(C.TAXNR,N'') AS TaxNumber,COALESCE(C.TAXOFFICE,N'') AS TaxOffice,CONCAT(C.ADDR1,N' ',C.ADDR2,N' ',C.TOWN) AS Address,COALESCE(C.CITY,N'') AS City,COALESCE(C.TELNRS1,N'') AS Phone,COALESCE(C.EMAILADDR,N'') AS Email,CAST(CASE WHEN COALESCE(C.ACTIVE,0)=0 THEN 1 ELSE 0 END AS bit) AS IsActive";

    public async Task<LogoCariRaporVM> AccountsAsync(LogoCariScopeVM scope, string search, bool includePassive, bool onlyBalance, int page, CancellationToken ct)
    {
        ValidatePage(page);
        if (search.Length > 150) throw new ArgumentException("Arama en fazla 150 karakter olabilir.");
        var sql = $"""
            WITH M AS ({MovementSource(scope)}),
            A AS (SELECT ClientRef,SUM(CASE WHEN [Date]<@start THEN Debit-Credit ELSE 0 END) AS OpeningBalance,
                   SUM(CASE WHEN [Date]>=@start THEN Debit ELSE 0 END) AS Debit,SUM(CASE WHEN [Date]>=@start THEN Credit ELSE 0 END) AS Credit,
                   SUM(Debit-Credit) AS ClosingBalance FROM M GROUP BY ClientRef),
            F AS (SELECT {ClientFields},COALESCE(A.OpeningBalance,0) AS OpeningBalance,COALESCE(A.Debit,0) AS Debit,
                   COALESCE(A.Credit,0) AS Credit,COALESCE(A.ClosingBalance,0) AS ClosingBalance
                  FROM {FirmTable(scope.Company.Number,"CLCARD")} C LEFT JOIN A ON A.ClientRef=C.LOGICALREF
                  WHERE (@includePassive=1 OR COALESCE(C.ACTIVE,0)=0) AND C.LOGICALREF>0
                    AND (@search=N'' OR CHARINDEX(@search,C.CODE)>0 OR CHARINDEX(@search,C.DEFINITION_)>0 OR CHARINDEX(@search,C.TAXNR)>0)
                    AND (@onlyBalance=0 OR COALESCE(A.ClosingBalance,0)<>0)),
            G AS (SELECT COUNT_BIG(*) AS TotalCount,COALESCE(SUM(Debit),0) AS TotalDebit,COALESCE(SUM(Credit),0) AS TotalCredit,
                  COALESCE(SUM(CASE WHEN ClosingBalance>0 THEN ClosingBalance ELSE 0 END),0) AS TotalDebitBalance,
                  COALESCE(SUM(CASE WHEN ClosingBalance<0 THEN -ClosingBalance ELSE 0 END),0) AS TotalCreditBalance FROM F)
            SELECT P.*,G.* FROM G OUTER APPLY(SELECT * FROM F ORDER BY Code,ClientRef OFFSET @skip ROWS FETCH NEXT 50 ROWS ONLY) P
            ORDER BY P.Code,P.ClientRef
            """;
        var rows = (await connection.QueryAsync<AccountResult>(Command(sql, new { periodBegin=scope.Period.BeginDate, start=scope.StartDate,
            endExclusive=scope.EndDate.AddDays(1), includePassive, onlyBalance, search=search.Trim(), skip=(long)(page-1)*50 }, ct))).ToList();
        var totals = rows.SingleOrDefault(x => x.ClientRef==0) ?? rows.FirstOrDefault() ?? new();
        return new() { Scope=scope,Search=search,IncludePassive=includePassive,OnlyBalance=onlyBalance,Page=page,TotalCount=totals.TotalCount,
            TotalDebit=totals.TotalDebit,TotalCredit=totals.TotalCredit,DebitBalance=totals.TotalDebitBalance,CreditBalance=totals.TotalCreditBalance,
            Rows=rows.Where(x=>x.ClientRef>0).Cast<LogoCariHesapVM>().ToList() };
    }
    private async Task<LogoCariHesapVM> ClientAsync(LogoCariScopeVM scope, int client, CancellationToken ct)
    {
        if(client<=0) throw new ArgumentException("Geçerli cari hesap seçiniz.");
        return await connection.QuerySingleOrDefaultAsync<LogoCariHesapVM>(Command($"SELECT {ClientFields} FROM {FirmTable(scope.Company.Number,"CLCARD")} C WHERE C.LOGICALREF=@client", new {client}, ct))
            ?? throw new KeyNotFoundException("Seçilen firmada cari hesap bulunamadı.");
    }
    public async Task<LogoCariEkstreVM> StatementAsync(LogoCariScopeVM scope, int client, int page, CancellationToken ct)
    {
        ValidatePage(page); var card = await ClientAsync(scope,client,ct);
        var sql = $"""
            WITH M AS ({MovementSource(scope)} AND L.CLIENTREF=@client),
            R AS(SELECT M.*,SUM(Debit-Credit) OVER(ORDER BY [Date],[Time],MovementRef ROWS UNBOUNDED PRECEDING) AS Balance FROM M),
            G AS(SELECT COUNT_BIG(CASE WHEN [Date]>=@start THEN 1 END) AS TotalCount,
                COALESCE(SUM(CASE WHEN [Date]<@start THEN Debit-Credit ELSE 0 END),0) AS OpeningBalance,
                COALESCE(SUM(CASE WHEN [Date]>=@start THEN Debit ELSE 0 END),0) AS TotalDebit,
                COALESCE(SUM(CASE WHEN [Date]>=@start THEN Credit ELSE 0 END),0) AS TotalCredit FROM M)
            SELECT P.*,G.* FROM G OUTER APPLY(SELECT * FROM R WHERE [Date]>=@start ORDER BY [Date],[Time],MovementRef OFFSET @skip ROWS FETCH NEXT 50 ROWS ONLY) P
            ORDER BY P.[Date],P.[Time],P.MovementRef
            """;
        var rows=(await connection.QueryAsync<MovementResult>(Command(sql,Args(scope,page,client),ct))).ToList(); var total=rows.FirstOrDefault() ?? new();
        return new() { Scope=scope,Client=card,Page=page,TotalCount=total.TotalCount,OpeningBalance=total.OpeningBalance,Debit=total.TotalDebit,Credit=total.TotalCredit,
            Rows=rows.Where(x=>x.MovementRef>0).Cast<LogoCariHareketVM>().ToList() };
    }
    private sealed class AccountResult : LogoCariHesapVM
    { public long TotalCount {get;set;} public decimal TotalDebit {get;set;} public decimal TotalCredit {get;set;} public decimal TotalDebitBalance {get;set;} public decimal TotalCreditBalance {get;set;} }
    private sealed class MovementResult : LogoCariHareketVM
    { public long TotalCount {get;set;} public decimal OpeningBalance {get;set;} public decimal TotalDebit {get;set;} public decimal TotalCredit {get;set;} }
}
