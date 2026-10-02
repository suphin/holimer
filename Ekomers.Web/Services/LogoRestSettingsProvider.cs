using Ekomers.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public sealed record LogoRestSettingsResolution(
    bool Success,
    LogoRestConnectionOptions? Options,
    string Message);

public sealed class LogoRestSettingsProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _protector;

    public LogoRestSettingsProvider(ApplicationDbContext context, IDataProtectionProvider protectionProvider)
    {
        _context = context;
        _protector = protectionProvider.CreateProtector("Ekomers.LogoRestSettings.v1");
    }

    public async Task<LogoRestSettingsResolution> GetAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _context.LogoRestApiSettings
            .AsNoTracking()
            .OrderByDescending(x => x.ID)
            .FirstOrDefaultAsync(cancellationToken);

        if (setting == null)
            return new(false, null, "Logo REST ayarları henüz kaydedilmemiş.");

        try
        {
            var password = _protector.Unprotect(setting.RestPasswordProtected);
            var clientSecret = string.IsNullOrWhiteSpace(setting.ClientSecretProtected)
                ? null
                : _protector.Unprotect(setting.ClientSecretProtected);

            return new(true, new LogoRestConnectionOptions(
                setting.ServerAddress,
                setting.Port,
                setting.Protocol,
                setting.RestUserName,
                password,
                setting.FirmNumber,
                setting.PeriodNumber,
                setting.ClientId,
                clientSecret), string.Empty);
        }
        catch
        {
            return new(false, null, "Kayıtlı Logo REST gizli bilgileri çözülemedi. Ayarlar ekranından şifreleri yeniden kaydedin.");
        }
    }
}
