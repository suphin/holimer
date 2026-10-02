using System.Globalization;
using System.Net;
using System.Text;
using ClosedXML.Excel;
using Ekomers.Common.Services;
using Ekomers.Data;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public sealed class LogoBankMovementReportJob
{
    private readonly ApplicationDbContext _context;
    private readonly LogoBankMovementReportService _reportService;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<LogoBankMovementReportJob> _logger;

    public LogoBankMovementReportJob(
        ApplicationDbContext context,
        LogoBankMovementReportService reportService,
        IEmailSenderService emailSender,
        ILogger<LogoBankMovementReportJob> logger)
    {
        _context = context;
        _reportService = reportService;
        _emailSender = emailSender;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task ProcessDueSchedulesAsync()
    {
        var now = DateTime.Now;
        var schedules = await _context.LogoBankMovementReportSchedules
            .Where(x => x.IsDelete != true && x.IsEnabled && x.SendTime <= now.TimeOfDay)
            .Where(x => !x.LastRunAt.HasValue || x.LastRunAt.Value.Date < now.Date)
            .OrderBy(x => x.SendTime)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            schedule.LastRunAt = now;
            schedule.UpdateDate = now;
            schedule.UpdateUserID = "Hangfire";
            await _context.SaveChangesAsync();
            try
            {
                await SendScheduleAsync(schedule.ID, "Zamanlanmış", "Hangfire", false);
            }
            catch (Exception ex)
            {
                schedule.LastError = Truncate(ex.Message, 4000);
                await _context.SaveChangesAsync();
                _logger.LogError(ex, "Zamanlanmış Logo banka raporu çalıştırılamadı. Plan: {ScheduleId}", schedule.ID);
            }
        }
    }

    public async Task SendScheduleAsync(
        int scheduleId,
        string triggerType = "Manuel",
        string? triggeredBy = null,
        bool updateLastRun = true)
    {
        var schedule = await _context.LogoBankMovementReportSchedules
            .FirstOrDefaultAsync(x => x.ID == scheduleId && x.IsDelete != true)
            ?? throw new InvalidOperationException("Rapor planı bulunamadı.");

        var now = DateTime.Now;
        if (updateLastRun)
        {
            schedule.LastRunAt = now;
            schedule.UpdateDate = now;
            schedule.UpdateUserID = triggeredBy;
            await _context.SaveChangesAsync();
        }

        // Alıcılar serbest e-posta adresi değildir; yalnızca aktif portal kullanıcılarıdır.
        var recipientUserIds = ParseStringIds(schedule.RecipientUserIds);
        var recipients = await _context.Users.AsNoTracking()
            .Where(x => recipientUserIds.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "")
            .Select(x => new { x.Id, x.AdSoyad, Email = x.Email! })
            .ToListAsync();
        if (recipients.Count == 0)
            throw new InvalidOperationException("Rapor planında e-posta adresi olan aktif bir portal kullanıcısı yok.");

        var companyIds = ParseIntegerIds(schedule.CompanyIds);
        var companies = await _context.Sirketler.AsNoTracking()
            .Where(x => companyIds.Contains(x.ID) && x.IsDelete != true && x.LogoTigerSirketKodu != null)
            .Select(x => new LogoBankMovementCompanyVM
            {
                Id = x.ID,
                Name = x.SirketKisaAdi ?? x.SirketAdi ?? ("Firma " + x.ID),
                FirmNumber = x.LogoTigerSirketKodu!
            })
            .ToListAsync();

        companies = companies.Where(x => int.TryParse(x.FirmNumber, out var firm) && firm is > 0 and <= 999).ToList();
        if (companies.Count == 0)
            throw new InvalidOperationException("Rapor planında Logo firma kodu tanımlı geçerli bir firma yok.");

        var today = now.Date;
        var start = schedule.IncludeToday ? today.AddDays(-(schedule.LookbackDays - 1)) : today.AddDays(-schedule.LookbackDays);
        var endExclusive = schedule.IncludeToday ? now : today;
        var recipientSummary = string.Join("; ", recipients.Select(x => $"{x.AdSoyad} <{x.Email}>"));

        var delivery = new LogoBankMovementReportDelivery
        {
            ScheduleId = schedule.ID,
            StartedAt = now,
            RangeStart = start,
            RangeEnd = endExclusive,
            CompanySummary = Truncate(string.Join(", ", companies.Select(x => $"{x.Name} ({x.FirmNumber})")), 1000)!,
            Recipients = Truncate(recipientSummary, 2000)!,
            TriggerType = Truncate(triggerType, 30)!,
            TriggeredBy = Truncate(triggeredBy, 256)
        };
        _context.LogoBankMovementReportDeliveries.Add(delivery);
        await _context.SaveChangesAsync();

        try
        {
            var rows = await _reportService.GetMovementsAsync(companies, start, endExclusive, "all", null);
            var incoming = rows.Where(x => x.Direction == "Gelen").Sum(x => x.LocalAmount);
            var outgoing = rows.Where(x => x.Direction == "Giden").Sum(x => x.LocalAmount);
            var subject = $"{schedule.Name} - {start:dd.MM.yyyy} / {endExclusive.AddTicks(-1):dd.MM.yyyy}";
            var body = BuildEmail(schedule.Name, companies, rows, start, endExclusive);
            var excelContent = BuildExcel(schedule.Name, companies, rows, start, endExclusive);
            var excelFileName = $"Logo_Banka_Hareketleri_{start:yyyyMMdd}_{endExclusive.AddTicks(-1):yyyyMMdd}.xlsx";

            var failed = new List<string>();
            foreach (var recipient in recipients)
            {
                if (!await _emailSender.SendEmailAsync(
                        recipient.Email,
                        subject,
                        body,
                        excelContent,
                        excelFileName,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                    failed.Add(recipient.AdSoyad);
            }

            if (failed.Count > 0)
                throw new InvalidOperationException($"E-posta gönderilemeyen portal kullanıcıları: {string.Join(", ", failed)}");

            delivery.RecordCount = rows.Count;
            delivery.IncomingTotalTry = incoming;
            delivery.OutgoingTotalTry = outgoing;
            delivery.CompletedAt = DateTime.Now;
            delivery.IsSuccess = true;
            schedule.LastSuccessAt = delivery.CompletedAt;
            schedule.LastRecordCount = rows.Count;
            schedule.LastError = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logo banka hareket raporu gönderilemedi. Plan: {ScheduleId}", schedule.ID);
            delivery.CompletedAt = DateTime.Now;
            delivery.IsSuccess = false;
            delivery.ErrorMessage = Truncate(ex.Message, 4000);
            schedule.LastError = Truncate(ex.Message, 4000);
            await _context.SaveChangesAsync();
            throw;
        }

        await _context.SaveChangesAsync();
    }

    public static List<int> ParseIntegerIds(string value) => value
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => int.TryParse(x, out var id) ? id : 0)
        .Where(x => x > 0).Distinct().ToList();

    public static List<string> ParseStringIds(string value) => value
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();

    private static string BuildEmail(
        string reportName,
        IReadOnlyCollection<LogoBankMovementCompanyVM> companies,
        IReadOnlyCollection<LogoBankMovementRowVM> rows,
        DateTime start,
        DateTime endExclusive)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var incoming = rows.Where(x => x.Direction == "Gelen").Sum(x => x.LocalAmount);
        var outgoing = rows.Where(x => x.Direction == "Giden").Sum(x => x.LocalAmount);
        var sb = new StringBuilder();
        sb.Append("<div style='font-family:Arial,sans-serif;color:#1f2937'>");
        sb.Append($"<h2 style='color:#245d46'>{WebUtility.HtmlEncode(reportName)}</h2>");
        sb.Append($"<p><strong>Dönem:</strong> {start:dd.MM.yyyy HH:mm} - {endExclusive.AddTicks(-1):dd.MM.yyyy HH:mm}<br>");
        sb.Append($"<strong>Firmalar:</strong> {WebUtility.HtmlEncode(string.Join(", ", companies.Select(x => x.Name)))}</p>");
        sb.Append("<table style='border-collapse:collapse;margin:18px 0'><tr>");
        sb.Append(SummaryCell("Gelen havale / EFT", incoming.ToString("N2", tr) + " TRY", "#e8f5ee"));
        sb.Append(SummaryCell("Giden havale / EFT", outgoing.ToString("N2", tr) + " TRY", "#fdecec"));
        sb.Append(SummaryCell("Net", (incoming - outgoing).ToString("N2", tr) + " TRY", "#edf2f7"));
        sb.Append(SummaryCell("Hareket", rows.Count.ToString("N0", tr), "#edf2f7"));
        sb.Append("</tr></table>");
        sb.Append("<p>Hareketlerin tamamı, filtrelenebilir detay tablosu ve özet sayfasıyla birlikte Excel eki olarak gönderilmiştir.</p>");
        sb.Append("<p style='color:#6b7280'>Bu rapor LogoConnection üzerinden salt okunur olarak oluşturulmuştur.</p></div>");
        return sb.ToString();
    }

    private static byte[] BuildExcel(
        string reportName,
        IReadOnlyCollection<LogoBankMovementCompanyVM> companies,
        IReadOnlyCollection<LogoBankMovementRowVM> rows,
        DateTime start,
        DateTime endExclusive)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Özet");
        var incoming = rows.Where(x => x.Direction == "Gelen").Sum(x => x.LocalAmount);
        var outgoing = rows.Where(x => x.Direction == "Giden").Sum(x => x.LocalAmount);

        summary.Cell("A1").Value = reportName;
        summary.Range("A1:D1").Merge();
        summary.Cell("A2").Value = "Dönem";
        summary.Cell("B2").Value = $"{start:dd.MM.yyyy HH:mm} - {endExclusive.AddTicks(-1):dd.MM.yyyy HH:mm}";
        summary.Cell("A3").Value = "Firmalar";
        summary.Cell("B3").Value = string.Join(", ", companies.Select(x => $"{x.Name} ({x.FirmNumber})"));
        summary.Cell("A5").Value = "Gelen havale / EFT";
        summary.Cell("B5").Value = incoming;
        summary.Cell("A6").Value = "Giden havale / EFT";
        summary.Cell("B6").Value = outgoing;
        summary.Cell("A7").Value = "Net hareket";
        summary.Cell("B7").Value = incoming - outgoing;
        summary.Cell("A8").Value = "Hareket sayısı";
        summary.Cell("B8").Value = rows.Count;
        var titleRange = summary.Range("A1:D1");
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#245D46");
        titleRange.Style.Font.FontColor = XLColor.White;
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        summary.Range("A5:A8").Style.Font.SetBold();
        summary.Range("B5:B7").Style.NumberFormat.Format = "#,##0.00 \"TRY\"";
        summary.Columns("A:D").AdjustToContents();
        summary.Column("B").Width = Math.Min(summary.Column("B").Width, 80);
        summary.SheetView.FreezeRows(1);

        var details = workbook.Worksheets.Add("Banka Hareketleri");
        var headers = new[]
        {
            "Firma", "Firma No", "Dönem", "Tarih", "Yön", "Banka Kodu", "Banka",
            "Hesap Kodu", "Hesap", "Hesap No", "Cari Kodu", "Cari Unvanı", "İşlem No",
            "Belge No", "Açıklama", "Tutar (TRY)", "İşlem Tutarı", "Döviz", "Kur"
        };
        for (var column = 0; column < headers.Length; column++)
            details.Cell(1, column + 1).Value = headers[column];

        var rowNumber = 2;
        foreach (var row in rows)
        {
            details.Cell(rowNumber, 1).Value = row.CompanyName;
            details.Cell(rowNumber, 2).Value = row.FirmNumber;
            details.Cell(rowNumber, 3).Value = row.PeriodNumber;
            details.Cell(rowNumber, 4).Value = row.Date;
            details.Cell(rowNumber, 5).Value = row.Direction;
            details.Cell(rowNumber, 6).Value = row.BankCode;
            details.Cell(rowNumber, 7).Value = row.BankName;
            details.Cell(rowNumber, 8).Value = row.AccountCode;
            details.Cell(rowNumber, 9).Value = row.AccountName;
            details.Cell(rowNumber, 10).Value = row.AccountNumber;
            details.Cell(rowNumber, 11).Value = row.ClientCode;
            details.Cell(rowNumber, 12).Value = row.ClientName;
            details.Cell(rowNumber, 13).Value = row.TransactionNumber;
            details.Cell(rowNumber, 14).Value = row.DocumentNumber;
            details.Cell(rowNumber, 15).Value = row.Description;
            details.Cell(rowNumber, 16).Value = row.LocalAmount;
            details.Cell(rowNumber, 17).Value = row.TransactionAmount;
            details.Cell(rowNumber, 18).Value = row.CurrencyCode;
            details.Cell(rowNumber, 19).Value = row.ExchangeRate;
            rowNumber++;
        }

        var headerRange = details.Range(1, 1, 1, headers.Length);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#245D46");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        details.Range(1, 1, Math.Max(1, rowNumber - 1), headers.Length).SetAutoFilter();
        details.SheetView.FreezeRows(1);
        details.Column(4).Style.DateFormat.Format = "dd.MM.yyyy";
        details.Columns(16, 17).Style.NumberFormat.Format = "#,##0.00";
        details.Column(19).Style.NumberFormat.Format = "#,##0.000000";
        details.Columns().AdjustToContents();
        foreach (var column in details.ColumnsUsed())
            column.Width = Math.Min(column.Width + 1, 45);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string SummaryCell(string title, string value, string color) =>
        $"<td style='padding:12px 18px;background:{color};border:4px solid white'><small>{title}</small><br><strong>{value}</strong></td>";

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? value : value.Length <= maxLength ? value : value[..maxLength];
}
