using Dapper;
using Ekomers.Models.ViewModels.Raporlar;

namespace Ekomers.Data.Services.Reports;

public sealed partial class LogoCustomerStatementService
{
    public async Task<LogoCariBelgeVM> DocumentAsync(LogoCariScopeVM scope, int client, int movement, int page, CancellationToken ct)
    {
        ValidatePage(page);
        if(movement<=0) throw new ArgumentException("Hareket referansı geçersiz.");
        var card=await ClientAsync(scope,client,ct);
        var item=await connection.QuerySingleOrDefaultAsync<LogoCariHareketVM>(Command(MovementSource(scope)+" AND L.CLIENTREF=@client AND L.LOGICALREF=@movement AND L.DATE_>=@start",Args(scope,page,client,movement),ct))
            ?? throw new KeyNotFoundException("Bu firma/dönem/cari ve tarih aralığında hareket bulunamadı.");
        var model=new LogoCariBelgeVM { Scope=scope,Client=card,Movement=item,Page=page,Number=item.Number,DocumentNumber=item.DocumentNumber,Date=item.Date,Description=item.Description };
        if(item.SourceRef<=0) { model.Notice="Kaynak belge referansı yok. Doğrulanmış cari hareket özeti gösteriliyor."; return model; }
        var p=new { source=item.SourceRef,client,movement,skip=(long)(page-1)*50 };
        DocumentHeader? header=null;
        switch(item.Module)
        {
            case 4:
                header=await connection.QuerySingleOrDefaultAsync<DocumentHeader>(Command($"SELECT FICHENO AS Number,DOCODE AS DocumentNumber,DATE_ AS [Date],NETTOTAL AS NetTotal,TOTALVAT AS VatTotal,TOTALDISCOUNTS AS DiscountTotal,CAST(CANCELLED AS bit) AS Cancelled,CAST(COALESCE(I.EINVOICE,0) AS bit) AS IsEInvoice,COALESCE(CONVERT(nvarchar(100),I.GUID),N'') AS EInvoiceGuid,{Notes("I")} AS Description FROM {Table(scope,"INVOICE")} I WHERE I.LOGICALREF=@source AND I.CLIENTREF=@client",p,ct));
                if(header==null) break;
                model.Kind="Fatura";model.LineMode="invoice";
                await DocumentRows(model,$"""
                    {Table(scope,"STLINE")} S
                    LEFT JOIN {FirmTable(scope.Company.Number,"ITEMS")} I ON I.LOGICALREF=S.STOCKREF AND S.LINETYPE NOT IN (2,3,4)
                    LEFT JOIN {FirmTable(scope.Company.Number,"SRVCARD")} V ON V.LOGICALREF=S.STOCKREF AND S.LINETYPE=4
                    LEFT JOIN {FirmTable(scope.Company.Number,"UNITSETL")} U ON U.LOGICALREF=S.UOMREF
                    WHERE S.INVOICEREF=@source AND COALESCE(S.CANCELLED,0)=0
                    """, """
                    S.LOGICALREF AS Ref,S.INVOICELNNO AS LineNumber,S.LINETYPE AS LineType,
                    COALESCE(I.CODE,V.CODE,N'') AS Code,COALESCE(I.NAME,V.DEFINITION_,N'') AS Name,
                    COALESCE(S.LINEEXP,N'') AS Description,CAST(S.AMOUNT AS decimal(28,6)) AS Quantity,
                    COALESCE(U.CODE,N'') AS Unit,CAST(S.PRICE AS decimal(28,6)) AS Price,
                    CAST(S.LINENET AS decimal(28,6)) AS Net,CAST(S.VAT AS decimal(28,6)) AS Vat,CAST(S.VATAMNT AS decimal(28,6)) AS VatAmount,CAST(COALESCE(S.VATINC,0) AS bit) AS VatIncluded
                    ""","S.INVOICELNNO,S.LOGICALREF","P.LineNumber,P.Ref",p,ct);
                break;
            case 7:
                // CLFLINE.SOURCEFREF is a BNFLINE reference, not a BNFICHE reference.
                var bank=await connection.QuerySingleOrDefaultAsync<BankSource>(Command($"SELECT B.SOURCEFREF AS HeaderRef FROM {Table(scope,"BNFLINE")} B WHERE B.LOGICALREF=@source AND B.CLIENTREF=@client",p,ct));
                if(bank==null) break;
                var bankArgs=new { source=bank.HeaderRef,client,skip=(long)(page-1)*50 };
                header=await connection.QuerySingleOrDefaultAsync<DocumentHeader>(Command($"SELECT FICHENO AS Number,N'' AS DocumentNumber,DATE_ AS [Date],CAST(CANCELLED AS bit) AS Cancelled,{Notes("F")} AS Description FROM {Table(scope,"BNFICHE")} F WHERE F.LOGICALREF=@source",bankArgs,ct));
                if(header==null) break;
                model.Kind="Banka fişi";model.LineMode="bank";model.Notice="Banka fişinin yalnızca seçilen cariye ait satırları gösterilir. Satır yönü banka hesabı bakışıdır; cari ekstre yönüyle ters olabilir.";
                await DocumentRows(model,$"""
                    {Table(scope,"BNFLINE")} B LEFT JOIN {FirmTable(scope.Company.Number,"BANKACC")} A ON A.LOGICALREF=B.BNACCREF
                    LEFT JOIN {FirmTable(scope.Company.Number,"BNCARD")} N ON N.LOGICALREF=A.BANKREF
                    WHERE B.SOURCEFREF=@source AND B.CLIENTREF=@client AND COALESCE(B.CANCELLED,0)=0
                    """, """
                    B.LOGICALREF AS Ref,B.LINENR AS LineNumber,COALESCE(A.CODE,N'') AS Code,
                    CONCAT(N.DEFINITION_,N' / ',A.DEFINITION_) AS Name,CONCAT(B.TRANNO,N' · ',B.DOCODE,N' · ',B.LINEEXP) AS Description,
                    CAST(B.AMOUNT AS decimal(28,6)) AS Net,COALESCE(B.TRCURR,0) AS CurrencyType,
                    CAST(CASE WHEN COALESCE(B.TRCURR,0) IN (0,160) THEN B.AMOUNT ELSE B.TRNET END AS decimal(28,6)) AS CurrencyAmount,
                    CAST(B.TRRATE AS decimal(28,8)) AS ExchangeRate,
                    CASE B.SIGN WHEN 0 THEN N'Borç (banka)' WHEN 1 THEN N'Alacak (banka)' ELSE N'Belirsiz' END AS Direction
                    ""","B.LINENR,B.LOGICALREF","P.LineNumber,P.Ref",bankArgs,ct);
                break;
            case 5:
                header=await connection.QuerySingleOrDefaultAsync<DocumentHeader>(Command($"SELECT FICHENO AS Number,DOCODE AS DocumentNumber,DATE_ AS [Date],CAST(CANCELLED AS bit) AS Cancelled,{Notes("F")} AS Description FROM {Table(scope,"CLFICHE")} F WHERE F.LOGICALREF=@source AND EXISTS(SELECT 1 FROM {Table(scope,"CLFLINE")} L WHERE L.LOGICALREF=@movement AND L.CLIENTREF=@client AND L.SOURCEFREF=F.LOGICALREF AND L.MODULENR=5)",p,ct));
                if(header==null) break;
                model.Kind="Cari hesap fişi / " + item.Kind;model.LineMode="client";model.Notice="Kaynak cari fişinin tüm satırları gösterilir; virman fişinde karşı cari de yer alabilir. Satır tutarları kendi cari hesaplarının borç/alacak yönündedir.";
                await DocumentRows(model,$"{Table(scope,"CLFLINE")} L LEFT JOIN {FirmTable(scope.Company.Number,"CLCARD")} C ON C.LOGICALREF=L.CLIENTREF WHERE L.MODULENR=5 AND L.SOURCEFREF=@source AND COALESCE(L.CANCELLED,0)=0", """
                    L.LOGICALREF AS Ref,L.LINENR AS LineNumber,COALESCE(C.CODE,N'') AS Code,COALESCE(C.DEFINITION_,N'') AS Name,COALESCE(L.LINEEXP,N'') AS Description,
                    CAST(CASE WHEN L.SIGN=0 THEN L.AMOUNT ELSE 0 END AS decimal(28,6)) AS Debit,
                    CAST(CASE WHEN L.SIGN=1 THEN L.AMOUNT ELSE 0 END AS decimal(28,6)) AS Credit,
                    COALESCE(L.TRCURR,0) AS CurrencyType,CAST(CASE WHEN COALESCE(L.TRCURR,0) IN (0,160) THEN L.AMOUNT ELSE L.TRNET END AS decimal(28,6)) AS CurrencyAmount,
                    CAST(L.TRRATE AS decimal(28,8)) AS ExchangeRate
                    ""","L.LINENR,L.LOGICALREF","P.LineNumber,P.Ref",p,ct);
                break;
            case 10:
                // KSLINES.TRANSREF points back to the validated CLFLINE. VCARDREF is not the client id.
                header=await connection.QuerySingleOrDefaultAsync<DocumentHeader>(Command($"SELECT FICHENO AS Number,DOCODE AS DocumentNumber,DATE_ AS [Date],CAST(CANCELLED AS bit) AS Cancelled,CONCAT(CUSTTITLE,N' · ',LINEEXP) AS Description FROM {Table(scope,"KSLINES")} K WHERE K.LOGICALREF=@source AND K.TRANSREF=@movement",p,ct));
                if(header==null) break;
                model.Kind="Kasa fişi";model.LineMode="bank";model.Notice="Kaynak kasa hareketi gösterilir. Satır yönü kasa hesabı bakışıdır.";
                await DocumentRows(model,$"{Table(scope,"KSLINES")} K WHERE K.LOGICALREF=@source AND K.TRANSREF=@movement", """
                    K.LOGICALREF AS Ref,1 AS LineNumber,CONVERT(nvarchar(100),K.CARDREF) AS Code,N'Kasa referansı' AS Name,
                    COALESCE(K.LINEEXP,N'') AS Description,CAST(K.AMOUNT AS decimal(28,6)) AS Net,COALESCE(K.TRCURR,0) AS CurrencyType,
                    CAST(CASE WHEN COALESCE(K.TRCURR,0) IN (0,160) THEN K.AMOUNT ELSE K.TRNET END AS decimal(28,6)) AS CurrencyAmount,
                    CAST(K.TRRATE AS decimal(28,8)) AS ExchangeRate,CASE K.SIGN WHEN 0 THEN N'Borç (kasa)' WHEN 1 THEN N'Alacak (kasa)' ELSE N'Belirsiz' END AS Direction
                    ""","K.LOGICALREF","P.Ref",p,ct);
                break;
            case 6:
                header=await connection.QuerySingleOrDefaultAsync<DocumentHeader>(Command($"SELECT ROLLNO AS Number,DOCODE AS DocumentNumber,DATE_ AS [Date],TOTAL AS NetTotal,CAST(CANCELLED AS bit) AS Cancelled,{Notes("R")} AS Description FROM {Table(scope,"CSROLL")} R WHERE R.LOGICALREF=@source AND R.CARDREF=@client",p,ct));
                if(header==null) break;
                model.Kind="Çek / senet bordrosu";model.LineMode="cheque";
                await DocumentRows(model,$"{Table(scope,"CSTRANS")} T JOIN {Table(scope,"CSCARD")} C ON C.LOGICALREF=T.CSREF WHERE T.ROLLREF=@source AND COALESCE(T.CANCELLED,0)=0", """
                    T.LOGICALREF AS Ref,T.LINENO_ AS LineNumber,COALESCE(C.PORTFOYNO,N'') AS Code,COALESCE(C.BANKNAME,N'') AS Name,
                    CONCAT(C.OWING,N' · ',N'İşlem durum kodu: ',T.STATUS) AS Description,COALESCE(C.SERINO,N'') AS SerialNumber,C.DUEDATE AS DueDate,
                    CAST(C.AMOUNT AS decimal(28,6)) AS Net,COALESCE(C.TRCURR,0) AS CurrencyType,
                    CAST(CASE WHEN COALESCE(C.TRCURR,0) IN (0,160) THEN C.AMOUNT ELSE C.TRNET END AS decimal(28,6)) AS CurrencyAmount,
                    CAST(C.TRRATE AS decimal(28,8)) AS ExchangeRate,CAST(COALESCE(C.CANCELLED,0) AS bit) AS Cancelled
                    ""","T.LINENO_,T.LOGICALREF","P.LineNumber,P.Ref",p,ct);
                break;
            default:
                model.Notice="Bu modül için belge satırı çözümlemesi henüz tanımlı değil. Doğrulanmış cari hareket bilgileri gösteriliyor; başka belge numarası tahmin edilmez.";
                return model;
        }
        if(header==null) { model.Notice="Kaynak belge bulunamadı veya cari bağlantısı doğrulanamadı. Belge silinmiş/taşınmış olabilir; yalnızca cari hareket özeti gösteriliyor."; return model; }
        model.Number=header.Number;model.DocumentNumber=header.DocumentNumber;model.Date=header.Date;model.Description=header.Description;
        model.NetTotal=header.NetTotal;model.VatTotal=header.VatTotal;model.DiscountTotal=header.DiscountTotal;model.Cancelled=header.Cancelled;
        model.IsEInvoice=header.IsEInvoice;model.EInvoiceGuid=header.EInvoiceGuid.Trim();
        return model;
    }

    private static string Notes(string alias) => $"CONCAT({alias}.GENEXP1,N' ',{alias}.GENEXP2,N' ',{alias}.GENEXP3,N' ',{alias}.GENEXP4,N' ',{alias}.GENEXP5,N' ',{alias}.GENEXP6)";
    private async Task DocumentRows(LogoCariBelgeVM model,string fromWhere,string projection,string sourceOrder,string resultOrder,object args,CancellationToken ct)
    {
        // One statement keeps page totals and rows on the same query; even an empty/out-of-range page keeps its count.
        var sql=$"SELECT P.*,G.TotalCount FROM (SELECT COUNT_BIG(*) AS TotalCount FROM {fromWhere}) G OUTER APPLY(SELECT {projection} FROM {fromWhere} ORDER BY {sourceOrder} OFFSET @skip ROWS FETCH NEXT 50 ROWS ONLY) P ORDER BY {resultOrder}";
        var rows=(await connection.QueryAsync<DocumentRow>(Command(sql,args,ct))).ToList();
        model.TotalCount=rows.FirstOrDefault()?.TotalCount ?? 0;model.Rows=rows.Where(x=>x.Ref>0).Cast<LogoCariBelgeSatirVM>().ToList();
    }
    private sealed class BankSource { public int HeaderRef {get;set;} }
    private sealed class DocumentHeader
    {
        public string Number {get;set;}=""; public string DocumentNumber {get;set;}="";public DateTime? Date {get;set;}
        public string Description {get;set;}="";public decimal? NetTotal {get;set;}public decimal? VatTotal {get;set;}public decimal? DiscountTotal {get;set;}public bool? Cancelled {get;set;}
        public bool IsEInvoice {get;set;} public string EInvoiceGuid {get;set;}="";
    }
    private sealed class DocumentRow : LogoCariBelgeSatirVM { public long TotalCount {get;set;} }
}
