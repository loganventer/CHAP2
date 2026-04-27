using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Auth;

public class BearerTokenRefreshMiddleware
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private readonly RequestDelegate _next;
    private readonly ILogger<BearerTokenRefreshMiddleware> _logger;

    public BearerTokenRefreshMiddleware(RequestDelegate next, ILogger<BearerTokenRefreshMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenStore tokens, IApiAuthClient api)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var stored = tokens.Read();
            if (stored is not null
                && !string.IsNullOrEmpty(stored.RefreshToken)
                && stored.ExpiresAtUtc <= DateTimeOffset.UtcNow.Add(RefreshSkew))
            {
                var refreshed = await api.RefreshAsync(stored.RefreshToken, context.RequestAborted);
                if (refreshed is not null)
                {
                    await tokens.WriteAsync(refreshed, context.RequestAborted);
                    _logger.LogDebug("Refreshed bearer token for {User}", context.User.Identity.Name);
                }
                else
                {
                    _logger.LogInformation("Bearer refresh failed for {User}; signing out", context.User.Identity.Name);
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }
        }
        await _next(context);
    }
}
