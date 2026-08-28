using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.Enums;

public enum PrdMaterialSource { [Display(Name = "Logo")] Logo = 1, [Display(Name = "Hızlı Kod")] QuickCode = 2 }
public enum PrdMaterialType { [Display(Name = "Hammadde")] RawMaterial = 1, [Display(Name = "Yarı Mamul")] SemiFinished = 2, [Display(Name = "Mamul")] FinishedProduct = 3, [Display(Name = "Ambalaj")] Packaging = 4, [Display(Name = "Diğer")] Other = 5 }
public enum PrdWarehouseType { Main = 1, Production = 2, FinishedProduct = 3, Quarantine = 4, Other = 5 }
public enum PrdRecipeStatus { [Display(Name = "Taslak")] Draft = 0, [Display(Name = "Aktif")] Active = 1, [Display(Name = "Pasif")] Passive = 2, [Display(Name = "Arşivlendi")] Archived = 3 }
public enum PrdProductionPlanStatus { Draft = 0, Approved = 1, ConvertedToOrder = 2, Cancelled = 3 }
public enum PrdProductionPlanHeaderStatus { Draft = 0, Calculated = 1, Locked = 2, ConvertedToOrders = 3, Cancelled = 4 }
public enum PrdProductionOrderStatus { Draft = 0, MaterialWaiting = 1, WarehousePreparing = 2, Ready = 3, InProduction = 4, Completed = 5, Cancelled = 6 }
public enum PrdReservationStatus { Active = 1, PartiallyUsed = 2, Used = 3, Released = 4, Cancelled = 5 }
public enum PrdWarehouseTaskStatus { Waiting = 0, Preparing = 1, Shortage = 2, Ready = 3, Shipped = 4, Delivered = 5, Cancelled = 6 }
public enum PrdStockDirection { In = 1, Out = 2 }
public enum PrdStockMovementType { Opening = 1, Purchase = 2, Transfer = 3, ProductionIssue = 4, ProductionReturn = 5, ProductionWaste = 6, ProductionReceipt = 7, Adjustment = 8 }
public enum PrdStockDocumentType { Opening = 1, WarehouseTask = 2, ProductionOrder = 3, ProductionActual = 4, Manual = 5 }

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
}
