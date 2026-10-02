using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels;

public sealed class LogoBankMovementReportPageVM
{
    public int CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Direction { get; set; } = "all";
    public string ClientSearch { get; set; } = string.Empty;
    public List<LogoBankMovementCompanyVM> Companies { get; set; } = [];
    public List<LogoBankMovementRecipientVM> AvailableRecipients { get; set; } = [];
    public List<LogoBankMovementRowVM> Rows { get; set; } = [];
    public List<LogoBankMovementReportScheduleRowVM> Schedules { get; set; } = [];
    public List<LogoBankMovementReportDeliveryRowVM> Deliveries { get; set; } = [];
    public LogoBankMovementScheduleFormVM ScheduleForm { get; set; } = new();
    public decimal IncomingTotalTry => Rows.Where(x => x.Direction == "Gelen").Sum(x => x.LocalAmount);
    public decimal OutgoingTotalTry => Rows.Where(x => x.Direction == "Giden").Sum(x => x.LocalAmount);
}

public sealed class LogoBankMovementCompanyVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FirmNumber { get; set; } = string.Empty;
}

public sealed class LogoBankMovementRecipientVM
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class LogoBankMovementRowVM
{
    public string CompanyName { get; set; } = string.Empty;
    public int FirmNumber { get; set; }
    public int PeriodNumber { get; set; }
    public long LogicalRef { get; set; }
    public DateTime Date { get; set; }
    public string Direction { get; set; } = string.Empty;
    public short TransactionCode { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public decimal LocalAmount { get; set; }
    public int CurrencyType { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal TransactionAmount { get; set; }
    public decimal ExchangeRate { get; set; }
}

public sealed class LogoBankMovementScheduleFormVM
{
    public int? Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "En az bir firma seçiniz.")]
    public List<int> CompanyIds { get; set; } = [];

    [MinLength(1, ErrorMessage = "En az bir portal kullanıcısı seçiniz.")]
    public List<string> RecipientUserIds { get; set; } = [];

    [Required]
    public string SendTime { get; set; } = "09:00";

    [Range(1, 31)]
    public int LookbackDays { get; set; } = 1;

    public bool IncludeToday { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}

public sealed class LogoBankMovementReportScheduleRowVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Companies { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
    public TimeSpan SendTime { get; set; }
    public int LookbackDays { get; set; }
    public bool IncludeToday { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public int? LastRecordCount { get; set; }
    public string? LastError { get; set; }
}

public sealed class LogoBankMovementReportDeliveryRowVM
{
    public long Id { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CompanySummary { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public decimal IncomingTotalTry { get; set; }
    public decimal OutgoingTotalTry { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
