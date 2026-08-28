using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.Enums;

public enum PrdMaterialSource { [Display(Name = "Logo")] Logo = 1, [Display(Name = "Hızlı Kod")] QuickCode = 2 }
public enum PrdMaterialType { [Display(Name = "Hammadde")] RawMaterial = 1, [Display(Name = "Yarı Mamul")] SemiFinished = 2, [Display(Name = "Mamul")] FinishedProduct = 3, [Display(Name = "Ambalaj")] Packaging = 4, [Display(Name = "Diğer")] Other = 5 }
public enum PrdWarehouseType { Main = 1, Production = 2, FinishedProduct = 3, Quarantine = 4, Other = 5, Scrap = 6 }
public enum PrdRecipeStatus { [Display(Name = "Taslak")] Draft = 0, [Display(Name = "Aktif")] Active = 1, [Display(Name = "Pasif")] Passive = 2, [Display(Name = "Arşivlendi")] Archived = 3 }
public enum PrdProductionPlanStatus { Draft = 0, Approved = 1, ConvertedToOrder = 2, Cancelled = 3 }
public enum PrdProductionPlanHeaderStatus { Draft = 0, Calculated = 1, Locked = 2, ConvertedToOrders = 3, Cancelled = 4 }
public enum PrdProductionOrderStatus { Draft = 0, MaterialWaiting = 1, WarehousePreparing = 2, Ready = 3, InProduction = 4, Completed = 5, Cancelled = 6 }
public enum PrdReservationStatus { Active = 1, PartiallyUsed = 2, Used = 3, Released = 4, Cancelled = 5 }
public enum PrdWarehouseTaskStatus { Waiting = 0, Preparing = 1, Shortage = 2, Ready = 3, Shipped = 4, Delivered = 5, Cancelled = 6 }
public enum PrdStockDirection { In = 1, Out = 2 }
public enum PrdStockMovementType { Opening = 1, Purchase = 2, Transfer = 3, ProductionIssue = 4, ProductionReturn = 5, ProductionWaste = 6, ProductionReceipt = 7, Adjustment = 8 }
public enum PrdStockDocumentType { Opening = 1, WarehouseTask = 2, ProductionOrder = 3, ProductionActual = 4, Manual = 5, InventoryDocument = 6 }
public enum PrdInventoryDocumentType { Opening = 1, PurchaseReceipt = 2, WarehouseTransfer = 3, ProductionIssue = 4, ProductionReturn = 5, ProductionReceipt = 6, ScrapTransfer = 7, ScrapDisposal = 8, Adjustment = 9, SupplierReturn = 10 }
public enum PrdInventoryDocumentStatus { Draft = 0, Posted = 1, Cancelled = 2, Reversed = 3 }
public enum PrdStockCostSource { Manual = 1, Opening = 2, ApprovedOffer = 3, LotAverage = 4, Transfer = 5, Production = 6, Adjustment = 7, LegacyImport = 8 }
public enum PrdQualityControlRequirement { [Display(Name = "Gerekli Değil")] NotRequired = 0, [Display(Name = "Zorunlu")] Required = 1, [Display(Name = "İsteğe Bağlı")] Optional = 2 }
public enum PrdQualityControlStatus { Pending = 0, Sampled = 1, Approved = 2, ConditionalApproval = 3, Rejected = 4 }

public static class ProductionEnumText
{
    public static string ToTurkish(this PrdRecipeStatus value) => value switch
    {
        PrdRecipeStatus.Draft => "Taslak", PrdRecipeStatus.Active => "Aktif",
        PrdRecipeStatus.Passive => "Pasif", PrdRecipeStatus.Archived => "Arşivlendi", _ => value.ToString()
    };

    public static string ToTurkish(this PrdMaterialType value) => value switch
    {
        PrdMaterialType.RawMaterial => "Hammadde", PrdMaterialType.SemiFinished => "Yarı Mamul",
        PrdMaterialType.FinishedProduct => "Mamul", PrdMaterialType.Packaging => "Ambalaj",
        PrdMaterialType.Other => "Diğer", _ => value.ToString()
    };

    public static string ToTurkish(this PrdMaterialSource value) =>
        value == PrdMaterialSource.Logo ? "Logo" : "Hızlı Kod";

    public static string ToTurkish(this PrdProductionPlanHeaderStatus value) => value switch
    {
        PrdProductionPlanHeaderStatus.Draft => "Taslak",
        PrdProductionPlanHeaderStatus.Calculated => "Hesaplandı",
        PrdProductionPlanHeaderStatus.Locked => "Kilitlendi",
        PrdProductionPlanHeaderStatus.ConvertedToOrders => "Emirlere Dönüştürüldü",
        PrdProductionPlanHeaderStatus.Cancelled => "İptal",
        _ => value.ToString()
    };

    public static string ToTurkish(this PrdProductionOrderStatus value) => value switch
    {
        PrdProductionOrderStatus.Draft => "Taslak", PrdProductionOrderStatus.MaterialWaiting => "Malzeme Bekliyor",
        PrdProductionOrderStatus.WarehousePreparing => "Depo Hazırlıyor", PrdProductionOrderStatus.Ready => "Üretime Hazır",
        PrdProductionOrderStatus.InProduction => "Üretimde", PrdProductionOrderStatus.Completed => "Tamamlandı",
        PrdProductionOrderStatus.Cancelled => "İptal", _ => value.ToString()
    };

    public static string ToTurkish(this PrdWarehouseType value) => value switch
    {
        PrdWarehouseType.Main => "Ana Depo", PrdWarehouseType.Production => "Üretim Deposu",
        PrdWarehouseType.FinishedProduct => "Mamul Deposu", PrdWarehouseType.Quarantine => "Karantina Deposu",
        PrdWarehouseType.Scrap => "Hurda Deposu", PrdWarehouseType.Other => "Diğer", _ => value.ToString()
    };

    public static string ToTurkish(this PrdInventoryDocumentType value) => value switch
    {
        PrdInventoryDocumentType.Opening => "Devir Girişi", PrdInventoryDocumentType.PurchaseReceipt => "Satın Alma Mal Kabul",
        PrdInventoryDocumentType.WarehouseTransfer => "Depolar Arası Transfer", PrdInventoryDocumentType.ProductionIssue => "Üretime Çıkış",
        PrdInventoryDocumentType.ProductionReturn => "Üretimden İade", PrdInventoryDocumentType.ProductionReceipt => "Üretimden Mamul Girişi",
        PrdInventoryDocumentType.ScrapTransfer => "Hurdaya Transfer", PrdInventoryDocumentType.ScrapDisposal => "Hurda İmha",
        PrdInventoryDocumentType.Adjustment => "Stok Düzeltme", PrdInventoryDocumentType.SupplierReturn => "Tedarikçiye İade", _ => value.ToString()
    };

    public static string ToTurkish(this PrdInventoryDocumentStatus value) => value switch
    {
        PrdInventoryDocumentStatus.Draft => "Taslak", PrdInventoryDocumentStatus.Posted => "İşlendi",
        PrdInventoryDocumentStatus.Cancelled => "İptal", PrdInventoryDocumentStatus.Reversed => "Ters Kayıtla Kapatıldı", _ => value.ToString()
    };
}
