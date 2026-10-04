using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Ekomers.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ekomers.Web.Infrastructure.Auth;

public static class ApiTokenDefaults
{
    public const string AuthenticationScheme = "ApiToken";
    public const string RecipeCostsReadScope = "RecipeCosts.Read";
    public const string LogoClientsReadScope = "LogoClients.Read";
}

public static class ApiTokenSecurity
{
    public static string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed class ApiTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApplicationDbContext _context;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext context)
        : base(options, logger, encoder)
    {
        _context = context;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var rawToken = authorization["Bearer ".Length..].Trim();
        if (rawToken.Length < 20)
            return AuthenticateResult.Fail("Geçersiz API anahtarı.");

        var tokenHash = ApiTokenSecurity.ComputeHash(rawToken);
        var token = await _context.ApiAccessTokens.FirstOrDefaultAsync(x =>
            x.TokenHash == tokenHash && x.IsDelete != true && x.IsActive == true,
            Context.RequestAborted);

        if (token == null)
            return AuthenticateResult.Fail("Geçersiz API anahtarı.");
        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value <= DateTime.Now)
            return AuthenticateResult.Fail("API anahtarının süresi dolmuş.");

        token.LastUsedAt = DateTime.Now;
        token.LastUsedIp = Context.Connection.RemoteIpAddress?.ToString();
        token.UseCount++;
        await _context.SaveChangesAsync(Context.RequestAborted);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, token.ID.ToString()),
            new Claim(ClaimTypes.Name, token.Name),
            new Claim("token_prefix", token.TokenPrefix)
        };
        claims.AddRange(token.Scope
            .Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(scope => new Claim("scope", scope)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ApiTokenDefaults.AuthenticationScheme));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, ApiTokenDefaults.AuthenticationScheme));
    }
}
