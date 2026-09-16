using System.ComponentModel.DataAnnotations;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class PriceRequestEmailComposeVM
{
    [Required(ErrorMessage = "Tedarikçi seçiniz.")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "E-posta taslağı seçiniz.")]
    public int TemplateId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ResponseDeadline { get; set; } = DateTime.Today.AddDays(7);

    [StringLength(1000)]
    public string? AdditionalNote { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderAccount { get; set; } = string.Empty;
    public string PreviewSubject { get; set; } = string.Empty;
    public string PreviewBodyHtml { get; set; } = string.Empty;
    public List<int> RequestLineIds { get; set; } = new();
    public List<PriceRequestEmailLineVM> Lines { get; set; } = new();
    public List<SelectListItem> Suppliers { get; set; } = new();
    public List<PriceRequestTemplateOptionVM> Templates { get; set; } = new();
    public List<PriceRequestSupplierOptionVM> SupplierOptions { get; set; } = new();
}

public sealed class PriceRequestEmailLineVM
{
    public int PurchaseRequestLineId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime? NeededDate { get; set; }
}

public sealed class PriceRequestTemplateOptionVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class PriceRequestSupplierOptionVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class PriceRequestEmailListVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public PurPriceRequestEmailStatus Status { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? ResponseDeadline { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class PurchasingEmailTemplateVM
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
