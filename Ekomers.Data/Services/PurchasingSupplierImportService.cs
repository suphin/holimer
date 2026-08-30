using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Ekomers.Data.Services;

public sealed class PurchasingSupplierImportService
{
    public const int PortalMinimumExclusiveId = 4466;
    public const string LogoCodePrefix = "320";

    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logo;

    public PurchasingSupplierImportService(ApplicationDbContext context, LogoContext logo)
    {
        _context = context;
        _logo = logo;
    }

    public async Task<SupplierImportPreviewVM> PreviewAsync(CancellationToken ct = default)
    {
        var source = await BuildCandidatesAsync(ct);
        var existing = await _context.PurSuppliers.AsNoTracking().ToListAsync(ct);
        var model = new SupplierImportPreviewVM
        {
            PortalMinimumExclusiveId = PortalMinimumExclusiveId,
            LogoCodePrefix = LogoCodePrefix,
            PortalRecordCount = source.PortalCount,
            LogoRecordCount = source.LogoCount
        };

        foreach (var candidate in source.Candidates.OrderBy(x => x.Code))
        {
            var match = FindExisting(candidate, existing);
            var invalid = string.IsNullOrWhiteSpace(candidate.Code) || string.IsNullOrWhiteSpace(candidate.Name);
            model.Lines.Add(new SupplierImportPreviewLineVM
            {
                Source = candidate.Source,
                SourceReference = candidate.SourceReference,
                Code = candidate.Code,
                Name = candidate.Name,
                TaxNumber = candidate.TaxNumber,
                Phone = candidate.Phone,
                Email = candidate.Email,
                LogoCode = candidate.LogoCode,
                IsActive = candidate.IsActive,
                ExistingSupplierId = match.Supplier?.ID,
                Status = invalid ? "Aktarılamaz" : match.Supplier == null ? "Yeni" : "Eşleşti",
                MatchReason = invalid ? "Firma kodu veya adı boş" : match.Reason
            });
        }

        model.CandidateCount = model.Lines.Count;
        model.NewCount = model.Lines.Count(x => x.Status == "Yeni");
        model.ExistingCount = model.Lines.Count(x => x.Status == "Eşleşti");
        model.InvalidCount = model.Lines.Count(x => x.Status == "Aktarılamaz");
        return model;
    }

    public async Task<SupplierImportResult> ImportAsync(string userId, CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var source = await BuildCandidatesAsync(ct);
        var existing = await _context.PurSuppliers.ToListAsync(ct);
        var now = DateTime.Now;
        var added = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var candidate in source.Candidates.OrderBy(x => x.Code))
        {
            if (string.IsNullOrWhiteSpace(candidate.Code) || string.IsNullOrWhiteSpace(candidate.Name))
            {
                skipped++;
                continue;
            }

            var match = FindExisting(candidate, existing).Supplier;
            if (match == null)
            {
                match = new PurSupplier
                {
                    Code = candidate.Code,
                    Name = candidate.Name,
                    TaxNumber = candidate.TaxNumber,
                    TaxOffice = candidate.TaxOffice,
                    Phone = candidate.Phone,
                    Email = candidate.Email,
                    Address = candidate.Address,
                    LogoCode = candidate.LogoCode,
                    Notes = $"Aktarım kaynağı: {candidate.SourceReference}",
                    IsActive = candidate.IsActive,
                    IsDelete = false,
                    CreateDate = now,
                    CreateUserID = userId
                };
                _context.PurSuppliers.Add(match);
                existing.Add(match);
                added++;
                continue;
            }

            var wasDeleted = match.IsDelete == true;
            if (candidate.HasLogoSource)
            {
                match.Name = candidate.Name;
                match.LogoCode = candidate.LogoCode;
                match.TaxNumber = Prefer(candidate.TaxNumber, match.TaxNumber);
                match.TaxOffice = Prefer(candidate.TaxOffice, match.TaxOffice);
                match.Phone = Prefer(candidate.Phone, match.Phone);
                match.Email = Prefer(candidate.Email, match.Email);
                match.Address = Prefer(candidate.Address, match.Address);
                match.IsActive = candidate.IsActive;
            }
            else
            {
                match.TaxNumber = Prefer(match.TaxNumber, candidate.TaxNumber);
                match.TaxOffice = Prefer(match.TaxOffice, candidate.TaxOffice);
                match.Phone = Prefer(match.Phone, candidate.Phone);
                match.Email = Prefer(match.Email, candidate.Email);
                match.Address = Prefer(match.Address, candidate.Address);
                if (wasDeleted) match.IsActive = candidate.IsActive;
            }
            match.IsDelete = false;
            match.DeleteDate = null;
            match.DeleteUserID = null;
            match.UpdateDate = now;
            match.UpdateUserID = userId;
            updated++;
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SupplierImportResult(added, updated, skipped);
    }

    private async Task<CandidateSource> BuildCandidatesAsync(CancellationToken ct)
    {
        var portal = await _context.Musteriler.AsNoTracking()
            .Where(x => x.ID > PortalMinimumExclusiveId && x.IsDelete != true)
            .OrderBy(x => x.ID)
            .ToListAsync(ct);
        var logo = await _logo.ClientCards.AsNoTracking()
            .Where(x => x.CODE != null && x.CODE.StartsWith(LogoCodePrefix))
            .OrderBy(x => x.CODE)
            .ToListAsync(ct);

        var candidates = logo.Select(x => new ImportCandidate
        {
            LogoLogicalRef = x.LOGICALREF,
            Code = Clean(x.CODE) ?? string.Empty,
            LogoCode = Clean(x.CODE),
            Name = Clean(x.DEFINITION_) ?? Clean(x.CODE) ?? string.Empty,
            TaxNumber = Clean(x.TAXNR),
            TaxOffice = Clean(x.TAXOFFICE),
            Phone = Clean(x.TELNRS1),
            Email = Clean(x.EMAILADDR),
            Address = JoinAddress(x.ADDR1, x.ADDR2, x.TOWN, x.CITY),
            IsActive = x.ACTIVE == 0,
            HasLogoSource = true,
            Source = "Logo",
            SourceReference = $"Logo {x.CODE} / LOGICALREF {x.LOGICALREF}"
        }).ToList();

        foreach (var item in portal)
        {
            ImportCandidate? candidate = null;
            if (int.TryParse(item.LOGICALREF, out var logicalRef))
                candidate = candidates.FirstOrDefault(x => x.LogoLogicalRef == logicalRef);
            candidate ??= FindCandidateByTaxOrName(item.VergiNo, item.SirketUnvan ?? item.AdSoyad, candidates);
            if (candidate == null)
            {
                candidates.Add(new ImportCandidate
                {
                    Code = $"MUS-{item.ID:D6}",
                    Name = Clean(item.SirketUnvan) ?? Clean(item.AdSoyad) ?? $"Müşteri {item.ID}",
                    TaxNumber = Clean(item.VergiNo),
                    TaxOffice = Clean(item.VergiDairesi),
                    Phone = Clean(item.Telefon),
                    Email = Clean(item.Eposta),
                    Address = JoinAddress(item.Adres, item.Ilce, item.Sehir),
                    IsActive = item.IsActive != false,
                    Source = "Musteriler",
                    SourceReference = $"Musteriler ID {item.ID}"
                });
                continue;
            }

            candidate.Source = candidate.HasLogoSource ? "Musteriler + Logo" : "Musteriler";
            candidate.SourceReference += $" / Musteriler ID {item.ID}";
            candidate.TaxNumber = Prefer(candidate.TaxNumber, item.VergiNo);
            candidate.TaxOffice = Prefer(candidate.TaxOffice, item.VergiDairesi);
            candidate.Phone = Prefer(candidate.Phone, item.Telefon);
            candidate.Email = Prefer(candidate.Email, item.Eposta);
            candidate.Address = Prefer(candidate.Address, JoinAddress(item.Adres, item.Ilce, item.Sehir));
        }

        return new CandidateSource(portal.Count, logo.Count, candidates);
    }

    private static (PurSupplier? Supplier, string? Reason) FindExisting(ImportCandidate candidate, IReadOnlyCollection<PurSupplier> existing)
    {
        var code = Normalize(candidate.Code);
        var logoCode = Normalize(candidate.LogoCode);
        var supplier = existing.FirstOrDefault(x => Normalize(x.Code) == code || (!string.IsNullOrEmpty(logoCode) && Normalize(x.LogoCode) == logoCode));
        if (supplier != null) return (supplier, "Firma/Logo kodu");
        var tax = NormalizeTax(candidate.TaxNumber);
        if (!string.IsNullOrEmpty(tax))
        {
            supplier = existing.FirstOrDefault(x => NormalizeTax(x.TaxNumber) == tax);
            if (supplier != null) return (supplier, "Vergi numarası");
        }
        var name = Normalize(candidate.Name);
        supplier = existing.FirstOrDefault(x => Normalize(x.Name) == name);
        return supplier == null ? (null, null) : (supplier, "Firma adı");
    }

    private static ImportCandidate? FindCandidateByTaxOrName(string? taxNumber, string? name, IEnumerable<ImportCandidate> candidates)
    {
        var tax = NormalizeTax(taxNumber);
        if (!string.IsNullOrEmpty(tax))
        {
            var taxMatch = candidates.FirstOrDefault(x => NormalizeTax(x.TaxNumber) == tax);
            if (taxMatch != null) return taxMatch;
        }
        var normalizedName = Normalize(name);
        return string.IsNullOrEmpty(normalizedName) ? null : candidates.FirstOrDefault(x => Normalize(x.Name) == normalizedName);
    }

    private static string? Prefer(string? primary, string? secondary) => Clean(primary) ?? Clean(secondary);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeTax(string? value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string? JoinAddress(params string?[] parts)
    {
        var values = parts.Select(Clean).Where(x => x != null).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return values.Count == 0 ? null : string.Join(" ", values!);
    }

    private sealed class ImportCandidate
    {
        public int? LogoLogicalRef { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? LogoCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public bool HasLogoSource { get; set; }
        public string Source { get; set; } = string.Empty;
        public string SourceReference { get; set; } = string.Empty;
    }

    private sealed record CandidateSource(int PortalCount, int LogoCount, List<ImportCandidate> Candidates);
}
