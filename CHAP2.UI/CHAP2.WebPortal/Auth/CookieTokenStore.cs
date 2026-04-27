using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace CHAP2.WebPortal.Auth;

public class CookieTokenStore : ITokenStore
{
    private readonly IHttpContextAccessor _accessor;

    public CookieTokenStore(IHttpContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public StoredTokens? Read()
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;

        var access = user.FindFirst(AuthClaimTypes.AccessToken)?.Value;
        if (string.IsNullOrEmpty(access)) return null;

        var refresh = user.FindFirst(AuthClaimTypes.RefreshToken)?.Value ?? string.Empty;
        var expiresStr = user.FindFirst(AuthClaimTypes.AccessTokenExpiresUtc)?.Value;
        DateTimeOffset.TryParse(expiresStr, out var expires);
        return new StoredTokens(access, refresh, expires);
    }

    public async Task WriteAsync(StoredTokens tokens, CancellationToken cancellationToken = default)
    {
        var ctx = _accessor.HttpContext
            ?? throw new InvalidOperationException("No HTTP context available to write tokens.");

        var existing = ctx.User;
        if (existing?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("Cannot refresh tokens for an unauthenticated request.");

        var keptClaims = existing.Claims
            .Where(c => c.Type != AuthClaimTypes.AccessToken
                     && c.Type != AuthClaimTypes.RefreshToken
                     && c.Type != AuthClaimTypes.AccessTokenExpiresUtc)
            .ToList();

        keptClaims.Add(new Claim(AuthClaimTypes.AccessToken, tokens.AccessToken));
        keptClaims.Add(new Claim(AuthClaimTypes.RefreshToken, tokens.RefreshToken));
        keptClaims.Add(new Claim(AuthClaimTypes.AccessTokenExpiresUtc, tokens.ExpiresAtUtc.ToString("O")));

        var identity = new ClaimsIdentity(keptClaims, existing.Identity.AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
        });
    }
}
