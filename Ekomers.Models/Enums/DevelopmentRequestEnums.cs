using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.Enums;

public enum DevelopmentRequestStatus
{
    [Display(Name = "Yeni")]
    New = 1,

    [Display(Name = "İşlemde")]
    InProgress = 2,

    [Display(Name = "Tamamlandı")]
    Completed = 3
}

public enum DevelopmentRequestPriority
{
    [Display(Name = "Düşük")]
    Low = 1,

    [Display(Name = "Normal")]
    Normal = 2,

    [Display(Name = "Yüksek")]
    High = 3,

    [Display(Name = "Acil")]
    Urgent = 4
}

public static class DevelopmentRequestEnumExtensions
{
    public static string ToDisplayText(this DevelopmentRequestStatus value) => value switch
    {
        DevelopmentRequestStatus.New => "Yeni",
        DevelopmentRequestStatus.InProgress => "İşlemde",
        DevelopmentRequestStatus.Completed => "Tamamlandı",
        _ => value.ToString()
    };

    public static string ToDisplayText(this DevelopmentRequestPriority value) => value switch
    {
        DevelopmentRequestPriority.Low => "Düşük",
        DevelopmentRequestPriority.Normal => "Normal",
        DevelopmentRequestPriority.High => "Yüksek",
        DevelopmentRequestPriority.Urgent => "Acil",
        _ => value.ToString()
    };
}
