using System.Security.Cryptography;
using Ekomers.Data;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ApiAnahtarlariController : Controller
{
    private readonly ApplicationDbContext _context;

    public ApiAnahtarlariController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Modul = "SistemYonetimi";
        var now = DateTime.Now;
        var rows = await _context.ApiAccessTokens.AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderByDescending(x => x.CreateDate)
            .Select(x => new ApiAccessTokenRowVM
            {
                Id = x.ID,
                Name = x.Name,
                TokenPrefix = x.TokenPrefix,
                Scope = x.Scope,
                CreatedAt = x.CreateDate ?? DateTime.MinValue,
                ExpiresAt = x.ExpiresAt,
                LastUsedAt = x.LastUsedAt,
                LastUsedIp = x.LastUsedIp,
                UseCount = x.UseCount,
                IsEnabled = x.IsActive == true && (!x.ExpiresAt.HasValue || x.ExpiresAt > now)
            })
            .ToListAsync(ct);

        return View(new ApiAccessTokenManagementVM
        {
            Tokens = rows,
            NewToken = TempData["NewApiToken"] as string
        });
    }

    [HttpGet]
    public IActionResult Test()
    {
        ViewBag.Modul = "SistemYonetimi";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(ApiAccessTokenCreateVM input, CancellationToken ct)
    {
        if (input.ExpiresAt.HasValue && input.ExpiresAt.Value.Date < DateTime.Today)
            ModelState.AddModelError(nameof(input.ExpiresAt), "Son kullanma tarihi geçmişte olamaz.");
        if (!ModelState.IsValid)
        {
            TempData["error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        var rawToken = "rct_" + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.Now;
        _context.ApiAccessTokens.Add(new ApiAccessToken
        {
            Name = input.Name.Trim(),
            TokenPrefix = rawToken[..Math.Min(12, rawToken.Length)],
            TokenHash = ApiTokenSecurity.ComputeHash(rawToken),
            Scope = $"{ApiTokenDefaults.RecipeCostsReadScope} {ApiTokenDefaults.LogoClientsReadScope}",
            ExpiresAt = input.ExpiresAt?.Date.AddDays(1),
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = User.Identity?.Name
        });
        await _context.SaveChangesAsync(ct);

        TempData["NewApiToken"] = rawToken;
        TempData["success"] = "API anahtarı oluşturuldu. Anahtar yalnızca bu kez gösterilecektir.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IptalEt(int id, CancellationToken ct)
    {
        var token = await _context.ApiAccessTokens.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (token == null)
            return NotFound();

        token.IsActive = false;
        token.UpdateDate = DateTime.Now;
        token.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "API anahtarı iptal edildi.";
        return RedirectToAction(nameof(Index));
    }
}
