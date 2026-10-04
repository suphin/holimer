using Ekomers.Models.ViewModels.Api;
using Ekomers.Web.Filters;
using Ekomers.Web.Infrastructure.Auth;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers.Api;

[ApiController]
[Route("api/v1/logo-clients")]
[Authorize(AuthenticationSchemes = ApiTokenDefaults.AuthenticationScheme, Policy = "LogoClientsApiRead")]
[ServiceFilter(typeof(ApiRequestAuditFilter))]
public sealed class LogoClientsApiController : ControllerBase
{
    private readonly LogoClientReadService _service;

    public LogoClientsApiController(LogoClientReadService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<LogoClientApiVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiPagedResponse<LogoClientApiVM>>> GetClients(
        int firmNumber,
        string? search = null,
        bool includePassive = false,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        if (firmNumber is < 1 or > 999)
            return BadRequest(new { error = "Firma numarası 1 ile 999 arasında olmalıdır." });
        if (search?.Length > 100)
            return BadRequest(new { error = "Arama ifadesi en fazla 100 karakter olabilir." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var result = await _service.GetClientsAsync(firmNumber, search, includePassive, page, pageSize, ct);
        return result == null
            ? NotFound(new { error = $"Logo firma {firmNumber:000} için cari kart tablosu bulunamadı." })
            : Ok(result);
    }
}
