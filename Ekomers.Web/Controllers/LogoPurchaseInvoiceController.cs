using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net;
using Ekomers.Data;
using Ekomers.Models.ViewModels.Purchasing;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrPurchasingOrUretim")]
public sealed class LogoPurchaseInvoiceController : Controller
{
    private const int DefaultPageSize = 25;
    private readonly LogoContext _logoContext;
    private readonly IELogoPostboxClient _eLogoPostboxClient;

    public LogoPurchaseInvoiceController(LogoContext logoContext, IELogoPostboxClient eLogoPostboxClient)
    {
        _logoContext = logoContext;
        _eLogoPostboxClient = eLogoPostboxClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? productSearch,
        string? invoiceNumber,
        string? supplierSearch,
        DateTime? startDate,
        DateTime? endDate,
        bool run = false,
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "YeniSatinalma";
        var today = DateTime.Today;
        var model = new LogoPurchaseInvoiceQueryVM
        {
            ProductSearch = Clean(productSearch),
            InvoiceNumber = Clean(invoiceNumber),
            SupplierSearch = Clean(supplierSearch),
            StartDate = startDate?.Date ?? today.AddDays(-90),
            EndDate = endDate?.Date ?? today,
            HasSearched = run,
            Page = Math.Max(1, page),
            PageSize = pageSize is >= 10 and <= 100 ? pageSize : DefaultPageSize
        };

        if (!run) return View(model);
        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
        {
            model.Error = "Bitiş tarihi başlangıç tarihinden önce olamaz.";
            return View(model);
        }

        try
        {
            await LoadInvoicesAsync(model, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            model.Error = "Logo satınalma faturaları okunamadı: " + ex.Message;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detay(
        string invoiceNumber,
        DateTime invoiceDate,
        string supplierCode,
        CancellationToken ct = default)
    {
        var model = new LogoPurchaseInvoiceDetailVM
        {
            InvoiceNumber = Clean(invoiceNumber) ?? string.Empty,
            InvoiceDate = invoiceDate.Date,
            SupplierCode = Clean(supplierCode) ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(model.InvoiceNumber) || model.InvoiceDate == default)
        {
            model.Error = "Fatura bilgileri geçersiz.";
            return PartialView("_Detay", model);
        }

        try
        {
            await LoadInvoiceDetailAsync(model, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            model.Error = "Logo fatura detayı okunamadı: " + ex.Message;
        }

        return PartialView("_Detay", model);
    }

    [HttpGet]
    public async Task<IActionResult> FaturaGorseli(
        string invoiceNumber,
        DateTime invoiceDate,
        string supplierCode,
        CancellationToken ct = default)
    {
        var invoice = new LogoPurchaseInvoiceDetailVM
        {
            InvoiceNumber = Clean(invoiceNumber) ?? string.Empty,
            InvoiceDate = invoiceDate.Date,
            SupplierCode = Clean(supplierCode) ?? string.Empty
        };
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber) || invoice.InvoiceDate == default)
            return PdfError("Fatura bilgileri geçersiz.", StatusCodes.Status400BadRequest);

        try
        {
            await LoadInvoiceDetailAsync(invoice, ct);
            if (invoice.Lines.Count == 0)
                return PdfError("Logo üzerinde satınalma faturası bulunamadı.", StatusCodes.Status404NotFound);
            if (!invoice.IsEInvoice || string.IsNullOrWhiteSpace(invoice.EInvoiceGuid))
                return PdfError("Bu kayıt için e-Fatura ETTN/UUID bilgisi bulunamadı.", StatusCodes.Status404NotFound);

            var document = await _eLogoPostboxClient.GetIncomingInvoicePdfAsync(invoice.EInvoiceGuid, ct);
            Response.Headers.ContentDisposition = $"inline; filename=\"{document.FileName}\"";
            Response.Headers.CacheControl = "private, no-store, max-age=0";
            return File(document.Content, document.ContentType);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return PdfError(ex.Message, StatusCodes.Status502BadGateway);
        }
    }

    private static ContentResult PdfError(string message, int statusCode) => new()
    {
        StatusCode = statusCode,
        ContentType = "text/html; charset=utf-8",
        Content = "<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><title>eLogo Fatura Görseli</title>" +
                  "<style>body{font-family:Arial,sans-serif;margin:40px;color:#3f4254}.error{padding:20px;border:1px solid #f5c2c7;background:#fff5f6;border-radius:8px;color:#b42318}</style>" +
                  "</head><body><div class=\"error\"><strong>Fatura görseli açılamadı.</strong><br>" +
                  WebUtility.HtmlEncode(message) + "</div></body></html>"
    };

    private async Task LoadInvoicesAsync(LogoPurchaseInvoiceQueryVM model, CancellationToken ct)
    {
        const string sql = """
            WITH MatchingInvoices AS
            (
                SELECT
                    CONVERT(date, [Tarihi]) AS [InvoiceDate],
                    [Fatura Numarası] AS [InvoiceNumber],
                    MAX([Fatura Belge Numarası]) AS [DocumentNumber],
                    [Cari Hesap Kodu] AS [SupplierCode],
                    [Cari Hesap Unvanı] AS [SupplierName],
                    COUNT(1) AS [MatchedLineCount],
                    COUNT(DISTINCT [Hizmet Kodu]) AS [MatchedProductCount],
                    SUM(CONVERT(decimal(38, 6), ISNULL([SatIr Matrahı], 0))) AS [MatchedNetTry],
                    MAX(CONVERT(decimal(38, 6), ISNULL([Fatura NET Toplam], 0))) AS [InvoiceNetTotalTry],
                    MAX(ISNULL(ei.[GUID], N'')) AS [EInvoiceGuid],
                    MAX(ISNULL(ei.[EINVOICE], 0)) AS [IsEInvoice],
                    MAX(ISNULL(ei.[ESTATUS], 0)) AS [EInvoiceStatusCode],
                    MAX(ISNULL([Fatura GİB Durumu], N'')) AS [GibStatus],
                    MAX(ISNULL([Doküman İzleme Numarası], N'')) AS [DocumentTrackingNumber]
                FROM [dbo].[FATURA_DOKUMU_100] f WITH (NOLOCK)
                OUTER APPLY
                (
                    SELECT TOP (1) i.[GUID], i.[EINVOICE], i.[ESTATUS]
                    FROM [dbo].[LG_100_01_INVOICE] i WITH (NOLOCK)
                    WHERE i.[FICHENO] = f.[Fatura Numarası]
                      AND CONVERT(date, i.[DATE_]) = CONVERT(date, f.[Tarihi])
                      AND i.[CANCELLED] = 0
                    ORDER BY i.[LOGICALREF] DESC
                ) ei
                WHERE [TUR] = N'Satınalma'
                  AND [Fatura Türü] = N'Satınalma Faturası'
                  AND [Fatura İptal Durumu] = N'İptal Edilmemiş'
                  AND (@productSearch IS NULL OR [Hizmet Kodu] LIKE @productLike ESCAPE '\' OR [Hizmet Açıklaması] LIKE @productLike ESCAPE '\')
                  AND (@invoiceNumber IS NULL OR [Fatura Numarası] LIKE @invoiceLike ESCAPE '\' OR [Fatura Belge Numarası] LIKE @invoiceLike ESCAPE '\')
                  AND (@supplierSearch IS NULL OR [Cari Hesap Kodu] LIKE @supplierLike ESCAPE '\' OR [Cari Hesap Unvanı] LIKE @supplierLike ESCAPE '\')
                  AND (@startDate IS NULL OR [Tarihi] >= @startDate)
                  AND (@endExclusive IS NULL OR [Tarihi] < @endExclusive)
                GROUP BY CONVERT(date, [Tarihi]), [Fatura Numarası], [Cari Hesap Kodu], [Cari Hesap Unvanı]
            )
            SELECT
                [InvoiceDate], [InvoiceNumber], [DocumentNumber], [SupplierCode], [SupplierName],
                [MatchedLineCount], [MatchedProductCount], [MatchedNetTry], [InvoiceNetTotalTry],
                [EInvoiceGuid], [IsEInvoice], [EInvoiceStatusCode], [GibStatus], [DocumentTrackingNumber],
                COUNT(1) OVER() AS [TotalCount]
            FROM MatchingInvoices
            ORDER BY [InvoiceDate] DESC, [InvoiceNumber] DESC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            """;

        var connection = _logoContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 20;
            AddParameter(command, "@productSearch", DbType.String, DbValue(model.ProductSearch));
            AddParameter(command, "@productLike", DbType.String, LikeValue(model.ProductSearch));
            AddParameter(command, "@invoiceNumber", DbType.String, DbValue(model.InvoiceNumber));
            AddParameter(command, "@invoiceLike", DbType.String, LikeValue(model.InvoiceNumber));
            AddParameter(command, "@supplierSearch", DbType.String, DbValue(model.SupplierSearch));
            AddParameter(command, "@supplierLike", DbType.String, LikeValue(model.SupplierSearch));
            AddParameter(command, "@startDate", DbType.DateTime2, model.StartDate ?? (object)DBNull.Value);
            AddParameter(command, "@endExclusive", DbType.DateTime2, model.EndDate?.AddDays(1) ?? (object)DBNull.Value);
            AddParameter(command, "@offset", DbType.Int32, (model.Page - 1) * model.PageSize);
            AddParameter(command, "@pageSize", DbType.Int32, model.PageSize);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                model.TotalCount = ReadInt32(reader, "TotalCount");
                model.Rows.Add(new LogoPurchaseInvoiceResultRowVM
                {
                    InvoiceDate = ReadDateTime(reader, "InvoiceDate"),
                    InvoiceNumber = ReadString(reader, "InvoiceNumber"),
                    DocumentNumber = ReadString(reader, "DocumentNumber"),
                    SupplierCode = ReadString(reader, "SupplierCode"),
                    SupplierName = ReadString(reader, "SupplierName"),
                    MatchedLineCount = ReadInt32(reader, "MatchedLineCount"),
                    MatchedProductCount = ReadInt32(reader, "MatchedProductCount"),
                    MatchedNetTry = ReadDecimal(reader, "MatchedNetTry"),
                    InvoiceNetTotalTry = ReadDecimal(reader, "InvoiceNetTotalTry"),
                    EInvoiceGuid = ReadString(reader, "EInvoiceGuid"),
                    IsEInvoice = ReadInt32(reader, "IsEInvoice") != 0,
                    EInvoiceStatusCode = ReadInt32(reader, "EInvoiceStatusCode"),
                    GibStatus = ReadString(reader, "GibStatus"),
                    DocumentTrackingNumber = ReadString(reader, "DocumentTrackingNumber")
                });
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private async Task LoadInvoiceDetailAsync(LogoPurchaseInvoiceDetailVM model, CancellationToken ct)
    {
        const string sql = """
            SELECT
                CONVERT(date, [Tarihi]) AS [InvoiceDate], [Fatura Numarası], [Fatura Belge Numarası],
                [Cari Hesap Kodu], [Cari Hesap Unvanı], [Hizmet Kodu], [Hizmet Açıklaması],
                [Birim], [İşlem Döviz Türü], [Miktar], [Birim Fiyat], [kur],
                [SatIr Matrahı], [Kdv], [Toplamı], [Fatura NET Toplam], [Ödeme Planı],
                [İrsaliye Numarası],
                TRY_CONVERT(date, NULLIF(LTRIM(RTRIM([İrsaliye Tarihi])), N''), 104) AS [DispatchDate],
                [SİPARİŞ Numarası],
                ISNULL(ei.[GUID], N'') AS [EInvoiceGuid],
                ISNULL(ei.[EINVOICE], 0) AS [IsEInvoice],
                ISNULL(ei.[ESTATUS], 0) AS [EInvoiceStatusCode],
                ISNULL([Fatura GİB Durumu], N'') AS [GibStatus],
                ISNULL([Doküman İzleme Numarası], N'') AS [DocumentTrackingNumber]
            FROM [dbo].[FATURA_DOKUMU_100] f WITH (NOLOCK)
            OUTER APPLY
            (
                SELECT TOP (1) i.[GUID], i.[EINVOICE], i.[ESTATUS]
                FROM [dbo].[LG_100_01_INVOICE] i WITH (NOLOCK)
                WHERE i.[FICHENO] = f.[Fatura Numarası]
                  AND CONVERT(date, i.[DATE_]) = CONVERT(date, f.[Tarihi])
                  AND i.[CANCELLED] = 0
                ORDER BY i.[LOGICALREF] DESC
            ) ei
            WHERE [TUR] = N'Satınalma'
              AND [Fatura Türü] = N'Satınalma Faturası'
              AND [Fatura İptal Durumu] = N'İptal Edilmemiş'
              AND [Fatura Numarası] = @invoiceNumber
              AND CONVERT(date, [Tarihi]) = @invoiceDate
              AND (@supplierCode IS NULL OR [Cari Hesap Kodu] = @supplierCode)
            ORDER BY [Hizmet Kodu], [Hizmet Açıklaması];
            """;

        var connection = _logoContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 20;
            AddParameter(command, "@invoiceNumber", DbType.String, model.InvoiceNumber);
            AddParameter(command, "@invoiceDate", DbType.Date, model.InvoiceDate);
            AddParameter(command, "@supplierCode", DbType.String, DbValue(model.SupplierCode));

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var quantity = ReadDecimal(reader, "Miktar");
                var netLineTry = ReadDecimal(reader, "SatIr Matrahı");
                model.Lines.Add(new LogoPurchaseInvoiceDetailRowVM
                {
                    ProductCode = ReadString(reader, "Hizmet Kodu"),
                    ProductName = ReadString(reader, "Hizmet Açıklaması"),
                    Quantity = quantity,
                    Unit = ReadString(reader, "Birim"),
                    LogoUnitPrice = ReadDecimal(reader, "Birim Fiyat"),
                    CurrencyCode = NormalizeCurrency(ReadString(reader, "İşlem Döviz Türü")),
                    ExchangeRate = ReadDecimal(reader, "kur"),
                    NetUnitPriceTry = quantity == 0 ? 0 : decimal.Abs(netLineTry / quantity),
                    NetLineTry = netLineTry,
                    VatTry = ReadDecimal(reader, "Kdv"),
                    TotalTry = ReadDecimal(reader, "Toplamı")
                });

                if (model.Lines.Count != 1) continue;
                model.InvoiceDate = ReadDateTime(reader, "InvoiceDate");
                model.InvoiceNumber = ReadString(reader, "Fatura Numarası");
                model.DocumentNumber = ReadString(reader, "Fatura Belge Numarası");
                model.SupplierCode = ReadString(reader, "Cari Hesap Kodu");
                model.SupplierName = ReadString(reader, "Cari Hesap Unvanı");
                model.PaymentPlan = ReadString(reader, "Ödeme Planı");
                model.DispatchNumber = ReadString(reader, "İrsaliye Numarası");
                model.DispatchDate = ReadNullableDateTime(reader, "DispatchDate");
                model.OrderNumber = ReadString(reader, "SİPARİŞ Numarası");
                model.InvoiceNetTotalTry = ReadDecimal(reader, "Fatura NET Toplam");
                model.EInvoiceGuid = ReadString(reader, "EInvoiceGuid");
                model.IsEInvoice = ReadInt32(reader, "IsEInvoice") != 0;
                model.EInvoiceStatusCode = ReadInt32(reader, "EInvoiceStatusCode");
                model.GibStatus = ReadString(reader, "GibStatus");
                model.DocumentTrackingNumber = ReadString(reader, "DocumentTrackingNumber");
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    private static object LikeValue(string? value) => string.IsNullOrWhiteSpace(value)
        ? DBNull.Value
        : $"%{EscapeLike(value)}%";

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);

    private static string ReadString(DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim() ?? string.Empty;
    }

    private static decimal ReadDecimal(DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        return value == DBNull.Value ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static int ReadInt32(DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        return value == DBNull.Value ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static DateTime ReadDateTime(DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        if (value == DBNull.Value) return DateTime.MinValue;
        if (value is DateTime date) return date;
        var text = Convert.ToString(value, CultureInfo.CurrentCulture);
        return DateTime.TryParse(text, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out date) ||
               DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date
            : throw new FormatException($"'{name}' alanındaki '{text}' değeri tarih olarak okunamadı.");
    }

    private static DateTime? ReadNullableDateTime(DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        if (value == DBNull.Value) return null;
        if (value is DateTime date) return date;
        var text = Convert.ToString(value, CultureInfo.CurrentCulture);
        return DateTime.TryParse(text, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out date) ||
               DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? date
            : null;
    }

    private static string NormalizeCurrency(string value) => value.Trim().ToUpperInvariant() switch
    {
        "TL" => "TRY",
        "EURO" => "EUR",
        var currency => currency
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
