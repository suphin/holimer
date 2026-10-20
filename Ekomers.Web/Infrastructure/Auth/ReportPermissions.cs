using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Ekomers.Web.Infrastructure.Auth;

/// <summary>One source for report endpoint policies, menus and action buttons.</summary>
public static class ReportPermissions
{
    public const string Bankalar = "Rapor.Bankalar";
    public const string BankaKredileri = "Rapor.BankaKredileri";
    public const string LogoBankaHareketleri = "Rapor.LogoBankaHareketleri";
    public const string BorcAlacak = "Rapor.BorcAlacak";
    public const string LogoCariEkstre = "Rapor.LogoCariEkstre";
    public const string SatisKarlilik = "Rapor.SatisKarlilik";
    public const string UrunMaliyetleri = "Rapor.UrunMaliyetleri";
    public const string UrunMaliyetleriYonet = "Rapor.UrunMaliyetleriYonet";
    public const string MuhasebeGiderleri = "Rapor.MuhasebeGiderleri";
    public const string MuhasebeGiderleriYonet = "Rapor.MuhasebeGiderleriYonet";

    public static IReadOnlyList<string> Policies { get; } = Array.AsReadOnly(new[] {
        Bankalar, BankaKredileri, LogoBankaHareketleri, BorcAlacak, LogoCariEkstre,
        SatisKarlilik, UrunMaliyetleri, UrunMaliyetleriYonet, MuhasebeGiderleri, MuhasebeGiderleriYonet
    });

    public static bool CanAccess(ClaimsPrincipal user, string policy)
    {
        if (user.Identity?.IsAuthenticated != true || !Policies.Contains(policy)) return false;
        if (user.IsInRole("Admin")) return true;
        if (user.HasClaim("Rapor", policy["Rapor.".Length..])) return true;
        // Managing costs implies their read-only pages, never the reverse or another report.
        return policy switch {
            UrunMaliyetleri => user.HasClaim("Rapor", "UrunMaliyetleriYonet"),
            MuhasebeGiderleri => user.HasClaim("Rapor", "MuhasebeGiderleriYonet"),
            _ => false
        };
    }

    public static bool CanSeeModule(ClaimsPrincipal user) => Policies.Any(policy => CanAccess(user, policy));

    // The legacy configurable runner is not an alternate route around page permissions.
    // New report keys must be explicitly mapped here before non-admin access is enabled.
    public static bool CanRun(ClaimsPrincipal user, string? reportKey) =>
        user.Identity?.IsAuthenticated == true && (user.IsInRole("Admin") ||
            (reportKey == "BorcAlacak" && CanAccess(user, BorcAlacak)));

    public static void AddPolicies(AuthorizationOptions options)
    {
        foreach (var policy in Policies)
            options.AddPolicy(policy, rule => rule.RequireAuthenticatedUser()
                .RequireAssertion(context => CanAccess(context.User, policy)));
    }
}
