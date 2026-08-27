namespace Ekomers.Models.Enums;

public enum PrdMaterialSource { Logo = 1, QuickCode = 2 }
public enum PrdMaterialType { RawMaterial = 1, SemiFinished = 2, FinishedProduct = 3, Packaging = 4, Other = 5 }
public enum PrdWarehouseType { Main = 1, Production = 2, FinishedProduct = 3, Quarantine = 4, Other = 5 }
public enum PrdRecipeStatus { Draft = 0, Active = 1, Passive = 2, Archived = 3 }
public enum PrdProductionPlanStatus { Draft = 0, Approved = 1, ConvertedToOrder = 2, Cancelled = 3 }
public enum PrdProductionOrderStatus { Draft = 0, MaterialWaiting = 1, WarehousePreparing = 2, Ready = 3, InProduction = 4, Completed = 5, Cancelled = 6 }
public enum PrdReservationStatus { Active = 1, PartiallyUsed = 2, Used = 3, Released = 4, Cancelled = 5 }
public enum PrdWarehouseTaskStatus { Waiting = 0, Preparing = 1, Shortage = 2, Ready = 3, Shipped = 4, Delivered = 5, Cancelled = 6 }
public enum PrdStockDirection { In = 1, Out = 2 }
public enum PrdStockMovementType { Opening = 1, Purchase = 2, Transfer = 3, ProductionIssue = 4, ProductionReturn = 5, ProductionWaste = 6, ProductionReceipt = 7, Adjustment = 8 }
public enum PrdStockDocumentType { Opening = 1, WarehouseTask = 2, ProductionOrder = 3, ProductionActual = 4, Manual = 5 }
