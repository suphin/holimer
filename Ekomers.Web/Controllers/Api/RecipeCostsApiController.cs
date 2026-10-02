using Ekomers.Data;
using Ekomers.Models.ViewModels.Api;
using Ekomers.Web.Filters;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers.Api;

[ApiController]
[Route("api/v1/recipe-costs")]
[Authorize(AuthenticationSchemes = ApiTokenDefaults.AuthenticationScheme, Policy = "RecipeCostsApiRead")]
[ServiceFilter(typeof(ApiRequestAuditFilter))]
public sealed class RecipeCostsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RecipeCostsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<RecipeCostScenarioApiVM>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiPagedResponse<RecipeCostScenarioApiVM>>> GetCalculations(
        string? productCode = null,
        DateTime? updatedAfter = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        productCode = productCode?.Trim();

        var query = _context.PrdRecipeCostScenarios.AsNoTracking().Where(x => x.IsDelete != true);
        if (updatedAfter.HasValue)
            query = query.Where(x => x.CalculationDate >= updatedAfter.Value);
        if (!string.IsNullOrWhiteSpace(productCode))
            query = query.Where(x => _context.PrdRecipeCostScenarioLines.Any(line =>
                line.ScenarioId == x.ID && line.IsDelete != true && line.ProductCode == productCode));

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(x => x.CalculationDate)
            .ThenByDescending(x => x.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RecipeCostScenarioApiVM
            {
                Id = x.ID,
                CalculationNumber = x.ScenarioNumber,
                Name = x.Name,
                CalculationDate = x.CalculationDate,
                DefaultProductionQuantity = x.DefaultProductionQuantity,
                TotalProductionQuantity = x.TotalProductionQuantity,
                RecipeCost = x.TotalRecipeCost,
                VariableCost = x.VariableCost,
                FixedCost = x.FixedCost,
                TotalCost = x.TotalCost,
                ProductCount = _context.PrdRecipeCostScenarioLines.Count(line => line.ScenarioId == x.ID && line.IsDelete != true),
                CreatedBy = x.CreateUserID ?? string.Empty
            })
            .ToListAsync(ct);

        return Ok(new ApiPagedResponse<RecipeCostScenarioApiVM>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            Data = data
        });
    }

    [HttpGet("calculations/{id:int}")]
    [ProducesResponseType(typeof(RecipeCostScenarioDetailApiVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeCostScenarioDetailApiVM>> GetCalculation(int id, CancellationToken ct)
    {
        var scenario = await _context.PrdRecipeCostScenarios.AsNoTracking()
            .Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new RecipeCostScenarioDetailApiVM
            {
                Id = x.ID,
                CalculationNumber = x.ScenarioNumber,
                Name = x.Name,
                CalculationDate = x.CalculationDate,
                DefaultProductionQuantity = x.DefaultProductionQuantity,
                TotalProductionQuantity = x.TotalProductionQuantity,
                RecipeCost = x.TotalRecipeCost,
                VariableCost = x.VariableCost,
                FixedCost = x.FixedCost,
                TotalCost = x.TotalCost,
                CreatedBy = x.CreateUserID ?? string.Empty
            })
            .FirstOrDefaultAsync(ct);
        if (scenario == null)
            return NotFound(new { error = "Maliyet çalışması bulunamadı." });

        var products = await GetProductsQuery()
            .Where(x => x.CalculationId == id)
            .OrderBy(x => x.ProductCode)
            .ToListAsync(ct);
        scenario.ProductCount = products.Count;
        scenario.Products = products;
        return Ok(scenario);
    }

    [HttpGet("products/{productCode}")]
    [ProducesResponseType(typeof(RecipeCostProductApiVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeCostProductApiVM>> GetLatestProductCost(string productCode, CancellationToken ct)
    {
        productCode = productCode.Trim();
        var result = await GetProductsQuery()
            .Where(x => x.ProductCode == productCode)
            .OrderByDescending(x => x.CalculationDate)
            .ThenByDescending(x => x.CalculationId)
            .FirstOrDefaultAsync(ct);

        return result == null
            ? NotFound(new { error = "Ürün için kaydedilmiş reçete maliyeti bulunamadı.", productCode })
            : Ok(result);
    }

    private IQueryable<RecipeCostProductApiVM> GetProductsQuery() =>
        from line in _context.PrdRecipeCostScenarioLines.AsNoTracking()
        join scenario in _context.PrdRecipeCostScenarios.AsNoTracking()
            on line.ScenarioId equals scenario.ID
        where line.IsDelete != true && scenario.IsDelete != true
        select new RecipeCostProductApiVM
        {
            CalculationId = scenario.ID,
            CalculationNumber = scenario.ScenarioNumber,
            CalculationDate = scenario.CalculationDate,
            RecipeVersionId = line.RecipeVersionId,
            ProductCode = line.ProductCode,
            ProductName = line.ProductName,
            ProductionQuantity = line.ProductionQuantity,
            ProductionUnit = line.ProductionUnit,
            RawMaterialUnitCost = line.RawMaterialUnitCost,
            PackagingUnitCost = line.PackagingUnitCost,
            RecipeUnitCost = line.RawMaterialUnitCost + line.PackagingUnitCost,
            RecipeTotalCost = line.RecipeMaterialCost,
            VariableCostShare = line.VariableCostShare,
            FixedCostShare = line.FixedCostShare,
            TotalCost = line.TotalCost,
            TotalUnitCost = line.TotalUnitCost
        };
}
