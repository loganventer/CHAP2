using System.Security.Claims;
using CHAP2.WebPortal.Auth;
using CHAP2.WebPortal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CHAP2.WebPortal.Controllers;

/// <summary>
/// Authentication endpoints. Single responsibility: validate the
/// configured username/password against AuthOptions and issue/clear
/// the cookie. Knows nothing about chorus data.
/// </summary>
public sealed class AccountController : Controller
{
    private readonly AuthOptions _auth;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IOptions<AuthOptions> auth,
        IPasswordHasher hasher,
        ILogger<AccountController> logger)
    {
        _auth = auth.Value;
        _hasher = hasher;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usernameOk = string.Equals(model.Username, _auth.Username, StringComparison.Ordinal);
        var passwordOk = _hasher.Verify(model.Password, _auth.PasswordHash);

        if (!(usernameOk && passwordOk))
        {
            _logger.LogWarning("Login failed for username '{User}'", model.Username);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new[] { new Claim(ClaimTypes.Name, _auth.Username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        return SafeLocalRedirect(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult SafeLocalRedirect(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
