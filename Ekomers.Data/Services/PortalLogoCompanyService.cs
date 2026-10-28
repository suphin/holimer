using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services;

public sealed record PortalLogoCompany(int Number,string Name);

/// <summary>Portal Şirketler records determine which Logo companies are used.</summary>
public sealed class PortalLogoCompanyService(ApplicationDbContext context)
{
    public async Task<List<PortalLogoCompany>> ListAsync(CancellationToken ct)
    {
        var rows=await context.Sirketler.AsNoTracking()
            .Where(x=>x.IsDelete!=true&&x.IsActive!=false&&x.LogoTigerSirketKodu!=null)
            .OrderBy(x=>x.ID)
            .Select(x=>new{x.ID,x.SirketKisaAdi,x.SirketAdi,x.LogoTigerSirketKodu}).ToListAsync(ct);
        var companies=new List<PortalLogoCompany>();
        foreach(var row in rows)
        {
            if(!int.TryParse(row.LogoTigerSirketKodu?.Trim(),NumberStyles.None,CultureInfo.InvariantCulture,out var number)||number is <1 or >999)continue;
            var name=!string.IsNullOrWhiteSpace(row.SirketKisaAdi)?row.SirketKisaAdi.Trim():row.SirketAdi?.Trim();
            companies.Add(new(number,string.IsNullOrWhiteSpace(name)?$"Firma {number:000}":name));
        }
        // A Logo firm is one reporting scope even when multiple portal cards refer to it.
        return companies.GroupBy(x=>x.Number).OrderBy(x=>x.Key)
            .Select(g=>new PortalLogoCompany(g.Key,string.Join(" / ",g.Select(x=>x.Name).Distinct(StringComparer.OrdinalIgnoreCase)))).ToList();
    }
}
