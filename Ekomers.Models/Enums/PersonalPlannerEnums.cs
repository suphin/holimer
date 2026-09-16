using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.Enums;

public enum PersonalWorkItemType
{
    [Display(Name = "Not")]
    Note = 0,
    [Display(Name = "Görev")]
    Task = 1
}

public enum PersonalWorkItemPriority
{
    [Display(Name = "Düşük")]
    Low = 0,
    [Display(Name = "Normal")]
    Normal = 1,
    [Display(Name = "Yüksek")]
    High = 2,
    [Display(Name = "Acil")]
    Urgent = 3
}

public enum PersonalWorkItemStatus
{
    [Display(Name = "Açık")]
    Open = 0,
    [Display(Name = "Tamamlandı")]
    Completed = 1,
    [Display(Name = "İptal")]
    Cancelled = 2
}
