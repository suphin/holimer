using Ekomers.Common.Services;
using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace Ekomers.Web.Controllers;

[Authorize]
public sealed class GelistirmeTalepleriController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Kullanici> _userManager;
    private readonly IEmailSenderService _emailSender;

    public GelistirmeTalepleriController(
        ApplicationDbContext context,
        UserManager<Kullanici> userManager,
        IEmailSenderService emailSender)
    {
        _context = context;
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, DevelopmentRequestStatus? status, CancellationToken ct)
    {
        ViewBag.Modul = "GelistirmeTalepleri";
        var isAdmin = User.IsInRole("Admin");
        var query = _context.DevelopmentRequests.AsNoTracking().Where(x => x.IsDelete != true);
        if (!isAdmin)
            query = query.Where(x => x.OwnerUserId == CurrentUserId);

        var scope = query;
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.RequestNumber.Contains(term) || x.Title.Contains(term) ||
                x.Description.Contains(term) || (isAdmin && x.OwnerName.Contains(term)));
        }

        var model = new DevelopmentRequestIndexVM
        {
            Search = search,
            Status = status,
            IsAdmin = isAdmin,
            NewCount = await scope.CountAsync(x => x.Status == DevelopmentRequestStatus.New, ct),
            InProgressCount = await scope.CountAsync(x => x.Status == DevelopmentRequestStatus.InProgress, ct),
            CompletedCount = await scope.CountAsync(x => x.Status == DevelopmentRequestStatus.Completed, ct),
            Requests = await query
                .OrderBy(x => x.Status == DevelopmentRequestStatus.Completed)
                .ThenByDescending(x => x.Priority)
                .ThenByDescending(x => x.CreateDate)
                .Select(x => new DevelopmentRequestRowVM
                {
                    Id = x.ID,
                    RequestNumber = x.RequestNumber,
                    OwnerName = x.OwnerName,
                    Title = x.Title,
                    Priority = x.Priority,
                    Status = x.Status,
                    CreatedAt = x.CreateDate ?? DateTime.MinValue,
                    UpdatedAt = x.UpdateDate,
                    CompletedAt = x.CompletedDate
                })
                .ToListAsync(ct)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Yeni()
    {
        ViewBag.Modul = "GelistirmeTalepleri";
        return View(new DevelopmentRequestCreateVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(DevelopmentRequestCreateVM model, CancellationToken ct)
    {
        ViewBag.Modul = "GelistirmeTalepleri";
        if (!Enum.IsDefined(typeof(DevelopmentRequestPriority), model.Priority))
            ModelState.AddModelError(nameof(model.Priority), "Geçerli bir öncelik seçiniz.");
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var now = DateTime.Now;
        var entity = new DevelopmentRequest
        {
            RequestNumber = $"GT-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..29].ToUpperInvariant(),
            OwnerUserId = user.Id,
            OwnerName = string.IsNullOrWhiteSpace(user.AdSoyad) ? (user.UserName ?? "Kullanıcı") : user.AdSoyad.Trim(),
            OwnerEmail = Clean(user.Email),
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Priority = model.Priority,
            Status = DevelopmentRequestStatus.New,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user.UserName
        };

        _context.DevelopmentRequests.Add(entity);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"{entity.RequestNumber} numaralı geliştirme talebiniz oluşturuldu.";
        return RedirectToAction(nameof(Detay), new { id = entity.ID });
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "GelistirmeTalepleri";
        var entity = await _context.DevelopmentRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (entity == null)
            return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && entity.OwnerUserId != CurrentUserId)
            return Forbid();

        return View(new DevelopmentRequestDetailVM
        {
            Id = entity.ID,
            RequestNumber = entity.RequestNumber,
            OwnerName = entity.OwnerName,
            OwnerEmail = entity.OwnerEmail,
            Title = entity.Title,
            Description = entity.Description,
            Priority = entity.Priority,
            Status = entity.Status,
            AdminNote = entity.AdminNote,
            CreatedAt = entity.CreateDate ?? DateTime.MinValue,
            UpdatedAt = entity.UpdateDate,
            CompletedAt = entity.CompletedDate,
            CompletedByName = entity.CompletedByName,
            NotificationSent = entity.NotificationSent,
            NotificationSentAt = entity.NotificationSentDate,
            NotificationError = entity.NotificationError,
            CanManage = isAdmin
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> DurumGuncelle(DevelopmentRequestAdminUpdateVM model, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(DevelopmentRequestStatus), model.Status))
            ModelState.AddModelError(nameof(model.Status), "Geçerli bir durum seçiniz.");
        if (!ModelState.IsValid)
        {
            TempData["error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Detay), new { id = model.Id });
        }

        var entity = await _context.DevelopmentRequests
            .FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (entity == null)
            return NotFound();

        var admin = await _userManager.GetUserAsync(User);
        var now = DateTime.Now;
        var completedNow = entity.Status != DevelopmentRequestStatus.Completed && model.Status == DevelopmentRequestStatus.Completed;

        entity.Status = model.Status;
        entity.AdminNote = Clean(model.AdminNote);
        entity.UpdateDate = now;
        entity.UpdateUserID = admin?.UserName ?? User.Identity?.Name;

        if (model.Status == DevelopmentRequestStatus.Completed)
        {
            entity.CompletedDate ??= now;
            entity.CompletedByUserId ??= admin?.Id;
            entity.CompletedByName ??= admin?.AdSoyad ?? admin?.UserName ?? User.Identity?.Name;
        }
        else
        {
            entity.CompletedDate = null;
            entity.CompletedByUserId = null;
            entity.CompletedByName = null;
            entity.NotificationSent = false;
            entity.NotificationSentDate = null;
            entity.NotificationError = null;
        }

        await _context.SaveChangesAsync(ct);

        if (completedNow)
            await SendCompletionEmailAsync(entity, ct);

        TempData["success"] = model.Status == DevelopmentRequestStatus.Completed
            ? "Talep tamamlandı olarak kapatıldı. Kullanıcı e-posta bildirimi işlendi."
            : $"Talep durumu {model.Status.ToDisplayText()} olarak güncellendi.";
        return RedirectToAction(nameof(Detay), new { id = entity.ID });
    }

    private async Task SendCompletionEmailAsync(DevelopmentRequest entity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.OwnerEmail))
        {
            entity.NotificationSent = false;
            entity.NotificationError = "Kullanıcının kayıtlı e-posta adresi bulunamadı.";
            await _context.SaveChangesAsync(ct);
            TempData["warning"] = "Talep kapatıldı ancak kullanıcının e-posta adresi bulunamadı.";
            return;
        }

        try
        {
            var detailUrl = Url.Action(nameof(Detay), "GelistirmeTalepleri", new { id = entity.ID }, Request.Scheme);
            var body = $"""
                <p>Merhaba {WebUtility.HtmlEncode(entity.OwnerName)},</p>
                <p><strong>{WebUtility.HtmlEncode(entity.RequestNumber)}</strong> numaralı geliştirme talebiniz tamamlandı.</p>
                <p><strong>Talep:</strong> {WebUtility.HtmlEncode(entity.Title)}</p>
                {(string.IsNullOrWhiteSpace(entity.AdminNote) ? string.Empty : $"<p><strong>Sonuç notu:</strong><br>{WebUtility.HtmlEncode(entity.AdminNote).Replace("\n", "<br>")}</p>")}
                {(string.IsNullOrWhiteSpace(detailUrl) ? string.Empty : $"<p><a href=\"{WebUtility.HtmlEncode(detailUrl)}\">Talebi görüntüleyin</a></p>")}
                <p>Bilginize.</p>
                """;

            var sent = await _emailSender.SendEmailAsync(
                entity.OwnerEmail,
                $"Geliştirme talebiniz tamamlandı - {entity.RequestNumber}",
                body);

            entity.NotificationSent = sent;
            entity.NotificationSentDate = sent ? DateTime.Now : null;
            entity.NotificationError = sent ? null : "E-posta servisi gönderimi başarısız olarak bildirdi.";
            if (!sent)
                TempData["warning"] = "Talep kapatıldı ancak e-posta gönderilemedi.";
        }
        catch (Exception ex)
        {
            entity.NotificationSent = false;
            entity.NotificationSentDate = null;
            entity.NotificationError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            TempData["warning"] = "Talep kapatıldı ancak e-posta gönderimi sırasında hata oluştu.";
        }

        await _context.SaveChangesAsync(ct);
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Oturum kullanıcı kimliği bulunamadı.");

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
