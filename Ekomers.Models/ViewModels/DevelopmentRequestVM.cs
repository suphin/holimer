using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels;

public sealed class DevelopmentRequestIndexVM
{
    public string? Search { get; set; }
    public DevelopmentRequestStatus? Status { get; set; }
    public bool IsAdmin { get; set; }
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public List<DevelopmentRequestRowVM> Requests { get; set; } = [];
}

public sealed class DevelopmentRequestRowVM
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DevelopmentRequestPriority Priority { get; set; }
    public DevelopmentRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class DevelopmentRequestCreateVM
{
    [Required(ErrorMessage = "Başlık zorunludur."), StringLength(200, MinimumLength = 5, ErrorMessage = "Başlık 5-200 karakter arasında olmalıdır.")]
    [Display(Name = "Talep başlığı")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Talep açıklaması zorunludur."), StringLength(4000, MinimumLength = 10, ErrorMessage = "Açıklama 10-4000 karakter arasında olmalıdır.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Öncelik")]
    public DevelopmentRequestPriority Priority { get; set; } = DevelopmentRequestPriority.Normal;
}

public sealed class DevelopmentRequestDetailVM
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevelopmentRequestPriority Priority { get; set; }
    public DevelopmentRequestStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public string? NotificationError { get; set; }
    public bool CanManage { get; set; }
}

public sealed class DevelopmentRequestAdminUpdateVM
{
    [Required]
    public int Id { get; set; }

    [Required]
    public DevelopmentRequestStatus Status { get; set; }

    [StringLength(4000)]
    public string? AdminNote { get; set; }
}
