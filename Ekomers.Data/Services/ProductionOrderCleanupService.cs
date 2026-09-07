using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace Ekomers.Data.Services;

public interface IProductionOrderCleanupService
{
    Task<ProductionOrderCleanupIndexVM> GetOrdersAsync(string? search, PrdProductionOrderStatus? status, CancellationToken ct);
    Task<ProductionOrderCleanupDetailVM?> GetPreviewAsync(int orderId, CancellationToken ct);
    Task<ProductionOrderCleanupResult> DeleteAsync(int orderId, string? confirmationOrderNumber, string? reason, bool confirmStockRollback, string? userName, CancellationToken ct);
}

public sealed class ProductionOrderCleanupService : IProductionOrderCleanupService
{
    private readonly ApplicationDbContext _context;

    public ProductionOrderCleanupService(ApplicationDbContext context) => _context = context;

    public async Task<ProductionOrderCleanupIndexVM> GetOrdersAsync(string? search, PrdProductionOrderStatus? status, CancellationToken ct)
    {
        search = search?.Trim();
        var query = from order in _context.PrdProductionOrders.AsNoTracking()
                    join plan in _context.PrdProductionPlans.AsNoTracking() on order.ProductionPlanId equals plan.ID
                    join header in _context.PrdProductionPlanHeaders.AsNoTracking() on plan.ProductionPlanHeaderId equals header.ID
                    join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID
                    join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID
                    where order.IsDelete != true
                    select new ProductionOrderCleanupListItemVM
                    {
                        Id = order.ID,
                        OrderNumber = order.OrderNumber,
                        PlanNumber = header.PlanNumber,
                        ProductCode = product.Code,
                        ProductName = product.Name,
                        PlannedQuantity = order.PlannedQuantity,
                        Unit = unit.Name,
                        BatchNumber = order.BatchNumber,
                        PlannedProductionDate = order.PlannedProductionDate,
                        CreatedDate = order.CreateDate,
                        Status = order.Status
                    };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => EF.Functions.Like(x.OrderNumber, "%" + search + "%") ||
                                     EF.Functions.Like(x.PlanNumber, "%" + search + "%") ||
                                     EF.Functions.Like(x.ProductCode, "%" + search + "%") ||
                                     EF.Functions.Like(x.ProductName, "%" + search + "%") ||
                                     EF.Functions.Like(x.BatchNumber, "%" + search + "%"));
        }

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var rows = await query.OrderByDescending(x => x.Id).Take(250).ToListAsync(ct);
        if (rows.Count == 0)
            return new ProductionOrderCleanupIndexVM { Search = search, Status = status, Orders = rows };

        var orderIds = rows.Select(x => x.Id).ToList();
        var requirementCounts = await _context.PrdMaterialRequirements.AsNoTracking()
            .Where(x => orderIds.Contains(x.ProductionOrderId) && x.IsDelete != true)
            .GroupBy(x => x.ProductionOrderId).Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count, ct);
        var taskCounts = await _context.PrdWarehouseTasks.AsNoTracking()
            .Where(x => orderIds.Contains(x.ProductionOrderId) && x.IsDelete != true)
            .GroupBy(x => x.ProductionOrderId).Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count, ct);
        var resultCounts = await _context.PrdProductionResults.AsNoTracking()
            .Where(x => orderIds.Contains(x.ProductionOrderId) && x.IsDelete != true)
            .GroupBy(x => x.ProductionOrderId).Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count, ct);

        var documentRows = await _context.PrdInventoryDocuments.AsNoTracking()
            .Where(x => x.SourceDocumentType == "ProductionOrder" && x.SourceDocumentId.HasValue && orderIds.Contains(x.SourceDocumentId.Value) && x.IsDelete != true)
            .Select(x => new { x.ID, OrderId = x.SourceDocumentId!.Value }).ToListAsync(ct);
        var documentOrderMap = documentRows.ToDictionary(x => x.ID, x => x.OrderId);
        var documentIds = documentOrderMap.Keys.ToList();
        var movementRows = await _context.PrdStockMovements.AsNoTracking()
            .Where(x => x.IsDelete != true &&
                        ((x.DocumentType == PrdStockDocumentType.ProductionOrder && x.DocumentId.HasValue && orderIds.Contains(x.DocumentId.Value)) ||
                         (x.InventoryDocumentId.HasValue && documentIds.Contains(x.InventoryDocumentId.Value))))
            .Select(x => new { x.ID, x.DocumentType, x.DocumentId, x.InventoryDocumentId }).ToListAsync(ct);
        var movementCounts = movementRows
            .Select(x => x.DocumentType == PrdStockDocumentType.ProductionOrder && x.DocumentId.HasValue && orderIds.Contains(x.DocumentId.Value)
                ? x.DocumentId.Value
                : x.InventoryDocumentId.HasValue && documentOrderMap.TryGetValue(x.InventoryDocumentId.Value, out var mappedOrderId) ? mappedOrderId : 0)
            .Where(x => x != 0).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

        foreach (var row in rows)
        {
            row.RequirementCount = requirementCounts.GetValueOrDefault(row.Id);
            row.WarehouseTaskCount = taskCounts.GetValueOrDefault(row.Id);
            row.StockMovementCount = movementCounts.GetValueOrDefault(row.Id);
            row.ProductionResultCount = resultCounts.GetValueOrDefault(row.Id);
        }

        return new ProductionOrderCleanupIndexVM { Search = search, Status = status, Orders = rows };
    }

    public async Task<ProductionOrderCleanupDetailVM?> GetPreviewAsync(int orderId, CancellationToken ct)
    {
        var header = await (from order in _context.PrdProductionOrders.AsNoTracking()
                            join plan in _context.PrdProductionPlans.AsNoTracking() on order.ProductionPlanId equals plan.ID
                            join planHeader in _context.PrdProductionPlanHeaders.AsNoTracking() on plan.ProductionPlanHeaderId equals planHeader.ID
                            join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID
                            join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID
                            where order.ID == orderId && order.IsDelete != true
                            select new ProductionOrderCleanupDetailVM
                            {
                                Id = order.ID,
                                OrderNumber = order.OrderNumber,
                                PlanNumber = planHeader.PlanNumber,
                                ProductCode = product.Code,
                                ProductName = product.Name,
                                PlannedQuantity = order.PlannedQuantity,
                                Unit = unit.Name,
                                BatchNumber = order.BatchNumber,
                                PlannedProductionDate = order.PlannedProductionDate,
                                CreatedDate = order.CreateDate,
                                Status = order.Status
                            }).FirstOrDefaultAsync(ct);
        if (header == null)
            return null;

        var graph = await LoadGraphAsync(orderId, ct);
        FillCounts(header, graph);
        header.StockImpacts = await BuildStockImpactsAsync(graph.ActiveMovements, ct);
        return header;
    }

    public async Task<ProductionOrderCleanupResult> DeleteAsync(int orderId, string? confirmationOrderNumber, string? reason, bool confirmStockRollback, string? userName, CancellationToken ct)
    {
        reason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 500)
            return ProductionOrderCleanupResult.Failure("Silme nedeni 5-500 karakter olmalıdır.");
        if (!confirmStockRollback)
            return ProductionOrderCleanupResult.Failure("Bağlı stok hareketlerinin geri alınacağını onaylamalısınız.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await _context.PrdProductionOrders.FirstOrDefaultAsync(x => x.ID == orderId && x.IsDelete != true, ct);
        if (order == null)
            return ProductionOrderCleanupResult.Failure("Üretim emri bulunamadı veya daha önce silinmiş.");
        if (!string.Equals(order.OrderNumber.Trim(), confirmationOrderNumber?.Trim(), StringComparison.OrdinalIgnoreCase))
            return ProductionOrderCleanupResult.Failure("Yazdığınız üretim emri numarası kayıtla eşleşmiyor.");

        var graph = await LoadGraphAsync(orderId, ct);
        var now = DateTime.Now;
        var actor = string.IsNullOrWhiteSpace(userName) ? "Admin" : userName.Trim();
        var activeMovementCount = graph.ActiveMovements.Count;
        var activeDependentCount = CountActiveDependencies(graph);

        MarkDeleted(graph.TaskLots, now, actor);
        MarkDeleted(graph.TaskItems, now, actor);
        MarkDeleted(graph.Tasks, now, actor);
        foreach (var task in graph.Tasks)
            task.Status = PrdWarehouseTaskStatus.Cancelled;

        MarkDeleted(graph.Reservations, now, actor);
        foreach (var reservation in graph.Reservations)
            reservation.Status = PrdReservationStatus.Cancelled;

        MarkDeleted(graph.Actuals, now, actor);
        MarkDeleted(graph.Results, now, actor);
        MarkDeleted(graph.StockMovements, now, actor);
        MarkDeleted(graph.InventoryDocumentLines, now, actor);
        MarkDeleted(graph.InventoryDocuments, now, actor);
        foreach (var document in graph.InventoryDocuments)
            document.Status = PrdInventoryDocumentStatus.Cancelled;

        MarkDeleted(graph.Requirements, now, actor);
        MarkDeleted([order], now, actor);
        order.Status = PrdProductionOrderStatus.Cancelled;

        var plan = await _context.PrdProductionPlans.FirstOrDefaultAsync(x => x.ID == order.ProductionPlanId, ct);
        if (plan != null && plan.Status != PrdProductionPlanStatus.Cancelled)
        {
            var hasAnotherActiveOrder = await _context.PrdProductionOrders.AnyAsync(
                x => x.ProductionPlanId == plan.ID && x.ID != order.ID && x.IsDelete != true, ct);
            if (!hasAnotherActiveOrder)
            {
                plan.IsConvertedToOrder = false;
                plan.Status = PrdProductionPlanStatus.Approved;
                plan.UpdateDate = now;
                plan.UpdateUserID = actor;
            }

            if (plan.ProductionPlanHeaderId.HasValue)
            {
                var planHeader = await _context.PrdProductionPlanHeaders.FirstOrDefaultAsync(x => x.ID == plan.ProductionPlanHeaderId.Value, ct);
                if (planHeader != null && planHeader.Status != PrdProductionPlanHeaderStatus.Cancelled)
                {
                    var headerPlanIds = await _context.PrdProductionPlans
                        .Where(x => x.ProductionPlanHeaderId == planHeader.ID && x.IsDelete != true)
                        .Select(x => x.ID).ToListAsync(ct);
                    var hasRemainingOrder = await _context.PrdProductionOrders.AnyAsync(
                        x => headerPlanIds.Contains(x.ProductionPlanId) && x.ID != order.ID && x.IsDelete != true, ct);
                    planHeader.Status = hasRemainingOrder
                        ? PrdProductionPlanHeaderStatus.ConvertedToOrders
                        : PrdProductionPlanHeaderStatus.Locked;
                    planHeader.UpdateDate = now;
                    planHeader.UpdateUserID = actor;
                }
            }
        }

        var auditPayload = JsonSerializer.Serialize(new
        {
            ProductionOrderId = order.ID,
            order.OrderNumber,
            Reason = reason,
            ActiveDependentRecordCount = activeDependentCount,
            CancelledStockMovementCount = activeMovementCount,
            DeletedAt = now
        });
        _context.UserActivityLog.Add(new UserActivityLog
        {
            DateTime = now,
            UserName = Limit(actor, 100),
            ControllerName = "UretimEmriYonetim",
            ActionName = "Sil",
            Parameters = Limit(auditPayload, 4096),
            Info = "ProductionOrderCascadeSoftDelete"
        });

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ProductionOrderCleanupResult.Success($"{order.OrderNumber} silindi. {activeDependentCount} bağlı kayıt kapatıldı; {activeMovementCount} stok hareketi bakiyeden çıkarıldı.");
    }

    private async Task<ProductionOrderGraph> LoadGraphAsync(int orderId, CancellationToken ct)
    {
        var requirements = await _context.PrdMaterialRequirements.Where(x => x.ProductionOrderId == orderId).ToListAsync(ct);
        var requirementIds = requirements.Select(x => x.ID).ToList();
        var reservations = await _context.PrdStockReservations.Where(x => requirementIds.Contains(x.MaterialRequirementId)).ToListAsync(ct);
        var reservationIds = reservations.Select(x => x.ID).ToList();
        var tasks = await _context.PrdWarehouseTasks.Where(x => x.ProductionOrderId == orderId).ToListAsync(ct);
        var taskIds = tasks.Select(x => x.ID).ToList();
        var taskItems = await _context.PrdWarehouseTaskItems
            .Where(x => taskIds.Contains(x.WarehouseTaskId) || requirementIds.Contains(x.MaterialRequirementId)).ToListAsync(ct);
        var taskItemIds = taskItems.Select(x => x.ID).ToList();
        var taskLots = await _context.PrdWarehouseTaskLots
            .Where(x => taskItemIds.Contains(x.WarehouseTaskItemId) || reservationIds.Contains(x.StockReservationId)).ToListAsync(ct);
        var actuals = await _context.PrdProductionMaterialActuals.Where(x => x.ProductionOrderId == orderId).ToListAsync(ct);
        var results = await _context.PrdProductionResults.Where(x => x.ProductionOrderId == orderId).ToListAsync(ct);
        var documents = await _context.PrdInventoryDocuments
            .Where(x => x.SourceDocumentType == "ProductionOrder" && x.SourceDocumentId == orderId).ToListAsync(ct);
        var documentIds = documents.Select(x => x.ID).ToList();
        var documentLines = await _context.PrdInventoryDocumentLines.Where(x => documentIds.Contains(x.InventoryDocumentId)).ToListAsync(ct);
        var movements = await _context.PrdStockMovements
            .Where(x => (x.DocumentType == PrdStockDocumentType.ProductionOrder && x.DocumentId == orderId) ||
                        (x.InventoryDocumentId.HasValue && documentIds.Contains(x.InventoryDocumentId.Value)))
            .ToListAsync(ct);

        return new ProductionOrderGraph(requirements, reservations, tasks, taskItems, taskLots, actuals, results, documents, documentLines, movements);
    }

    private async Task<List<ProductionOrderCleanupStockImpactVM>> BuildStockImpactsAsync(IReadOnlyCollection<PrdStockMovement> movements, CancellationToken ct)
    {
        if (movements.Count == 0)
            return [];

        var materialIds = movements.Select(x => x.MaterialId).Distinct().ToList();
        var warehouseIds = movements.Select(x => x.WarehouseId).Distinct().ToList();
        var unitIds = movements.Select(x => x.UnitId).Distinct().ToList();
        var materials = await _context.PrdMaterials.AsNoTracking().Where(x => materialIds.Contains(x.ID)).ToDictionaryAsync(x => x.ID, ct);
        var warehouses = await _context.PrdWarehouses.AsNoTracking().Where(x => warehouseIds.Contains(x.ID)).ToDictionaryAsync(x => x.ID, ct);
        var units = await _context.PrdUnits.AsNoTracking().Where(x => unitIds.Contains(x.ID)).ToDictionaryAsync(x => x.ID, ct);

        return movements.GroupBy(x => new { x.MaterialId, x.WarehouseId, x.UnitId })
            .Select(g => new ProductionOrderCleanupStockImpactVM
            {
                MaterialCode = materials.TryGetValue(g.Key.MaterialId, out var material) ? material.Code : g.Key.MaterialId.ToString(),
                MaterialName = material?.Name ?? "Silinmiş malzeme",
                WarehouseCode = warehouses.TryGetValue(g.Key.WarehouseId, out var warehouse) ? warehouse.Code : g.Key.WarehouseId.ToString(),
                WarehouseName = warehouse?.Name ?? "Silinmiş depo",
                Unit = units.TryGetValue(g.Key.UnitId, out var unit) ? unit.Name : string.Empty,
                CurrentMovementContribution = g.Sum(x => x.Direction == PrdStockDirection.In ? x.Quantity : -x.Quantity)
            })
            .Where(x => x.CurrentMovementContribution != 0)
            .OrderBy(x => x.MaterialCode).ThenBy(x => x.WarehouseCode).ToList();
    }

    private static void FillCounts(ProductionOrderCleanupDetailVM model, ProductionOrderGraph graph)
    {
        model.RequirementCount = ActiveCount(graph.Requirements);
        model.ReservationCount = ActiveCount(graph.Reservations);
        model.WarehouseTaskCount = ActiveCount(graph.Tasks);
        model.WarehouseTaskItemCount = ActiveCount(graph.TaskItems);
        model.WarehouseTaskLotCount = ActiveCount(graph.TaskLots);
        model.ProductionActualCount = ActiveCount(graph.Actuals);
        model.ProductionResultCount = ActiveCount(graph.Results);
        model.InventoryDocumentCount = ActiveCount(graph.InventoryDocuments);
        model.InventoryDocumentLineCount = ActiveCount(graph.InventoryDocumentLines);
        model.StockMovementCount = graph.ActiveMovements.Count;
        model.TotalDependentRecordCount = CountActiveDependencies(graph);
    }

    private static int CountActiveDependencies(ProductionOrderGraph graph) =>
        ActiveCount(graph.Requirements) + ActiveCount(graph.Reservations) + ActiveCount(graph.Tasks) +
        ActiveCount(graph.TaskItems) + ActiveCount(graph.TaskLots) + ActiveCount(graph.Actuals) +
        ActiveCount(graph.Results) + ActiveCount(graph.InventoryDocuments) + ActiveCount(graph.InventoryDocumentLines) +
        graph.ActiveMovements.Count;

    private static int ActiveCount<T>(IEnumerable<T> rows) where T : BaseEntity => rows.Count(x => x.IsDelete != true);

    private static void MarkDeleted<T>(IEnumerable<T> rows, DateTime now, string userName) where T : BaseEntity
    {
        foreach (var row in rows.Where(x => x.IsDelete != true))
        {
            row.IsDelete = true;
            row.IsActive = false;
            row.DeleteDate = now;
            row.DeleteUserID = userName;
            row.UpdateDate = now;
            row.UpdateUserID = userName;
        }
    }

    private static string Limit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ProductionOrderGraph(
        List<PrdMaterialRequirement> Requirements,
        List<PrdStockReservation> Reservations,
        List<PrdWarehouseTask> Tasks,
        List<PrdWarehouseTaskItem> TaskItems,
        List<PrdWarehouseTaskLot> TaskLots,
        List<PrdProductionMaterialActual> Actuals,
        List<PrdProductionResult> Results,
        List<PrdInventoryDocument> InventoryDocuments,
        List<PrdInventoryDocumentLine> InventoryDocumentLines,
        List<PrdStockMovement> StockMovements)
    {
        public List<PrdStockMovement> ActiveMovements => StockMovements.Where(x => x.IsDelete != true).ToList();
    }
}
