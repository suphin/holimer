namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class LogoPurchaseInvoiceQueryVM
{
    public string? ProductSearch { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? SupplierSearch { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool HasSearched { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public string? Error { get; set; }
    public List<LogoPurchaseInvoiceResultRowVM> Rows { get; set; } = [];

    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class LogoPurchaseInvoiceResultRowVM
{
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public int MatchedLineCount { get; set; }
    public int MatchedProductCount { get; set; }
    public decimal MatchedNetTry { get; set; }
    public decimal InvoiceNetTotalTry { get; set; }
    public string EInvoiceGuid { get; set; } = string.Empty;
    public bool IsEInvoice { get; set; }
    public int EInvoiceStatusCode { get; set; }
    public string GibStatus { get; set; } = string.Empty;
    public string DocumentTrackingNumber { get; set; } = string.Empty;
}

public sealed class LogoPurchaseInvoiceDetailVM
{
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string PaymentPlan { get; set; } = string.Empty;
    public string DispatchNumber { get; set; } = string.Empty;
    public DateTime? DispatchDate { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal InvoiceNetTotalTry { get; set; }
    public string EInvoiceGuid { get; set; } = string.Empty;
    public bool IsEInvoice { get; set; }
    public int EInvoiceStatusCode { get; set; }
    public string GibStatus { get; set; } = string.Empty;
    public string DocumentTrackingNumber { get; set; } = string.Empty;
    public string? Error { get; set; }
    public List<LogoPurchaseInvoiceDetailRowVM> Lines { get; set; } = [];
}

public sealed class LogoPurchaseInvoiceDetailRowVM
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal LogoUnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal NetUnitPriceTry { get; set; }
    public decimal NetLineTry { get; set; }
    public decimal VatTry { get; set; }
    public decimal TotalTry { get; set; }
}
