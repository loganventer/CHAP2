using System.Security.Claims;
using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Auth;
using CHAP2.WebPortal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CHAP2.WebPortal.Controllers;

public sealed class AccountController : Controller
{
    private readonly IApiAuthClient _api;
    private readonly ITokenStore _tokens;
    private readonly ApiAuthSettings _settings;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IApiAuthClient api,
        ITokenStore tokens,
        IOptions<ApiAuthSettings> settings,
        ILogger<AccountController> logger)
    {
        _api = api;
        _tokens = tokens;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var outcome = await _api.LoginAsync(new LoginRequestDto { UserName = model.Username, Password = model.Password }, cancellationToken);
        if (!outcome.Succeeded || outcome.Tokens is null || outcome.User is null)
        {
            foreach (var err in outcome.Errors) ModelState.AddModelError(string.Empty, err);
            if (outcome.Errors.Count == 0) ModelState.AddModelError(string.Empty, "Login failed.");
            return View(model);
        }

        await SignInAsync(outcome.User, outcome.Tokens);

        if (outcome.User.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword), new { forced = true });

        return SafeLocalRedirect(model.ReturnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var outcome = await _api.RegisterAsync(new RegisterRequestDto
        {
            UserName = model.UserName,
            Email = model.Email,
            Password = model.Password,
        }, cancellationToken);

        if (!outcome.Succeeded)
        {
            foreach (var err in outcome.Errors) ModelState.AddModelError(string.Empty, err);
            return View(model);
        }

        TempData["AccountMessage"] = "Account created. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ChangePassword(bool forced = false) => View(new ChangePasswordViewModel { Forced = forced });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var stored = _tokens.Read();
        if (stored is null) return RedirectToAction(nameof(Login));

        var outcome = await _api.ChangePasswordAsync(stored.AccessToken, new ChangePasswordRequestDto
        {
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword,
        }, cancellationToken);

        if (!outcome.Succeeded)
        {
            foreach (var err in outcome.Errors) ModelState.AddModelError(string.Empty, err);
            return View(model);
        }

        var refreshedUser = await _api.GetMeAsync(stored.AccessToken, cancellationToken);
        if (refreshedUser is not null) await SignInAsync(refreshedUser, stored);

        TempData["AccountMessage"] = "Password changed.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);
        await _api.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = model.Email }, cancellationToken);
        TempData["AccountMessage"] = "If an account exists for that email, a reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null) =>
        View(new ResetPasswordViewModel { Email = email ?? string.Empty, ResetToken = token ?? string.Empty });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var outcome = await _api.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Email = model.Email,
            ResetToken = model.ResetToken,
            NewPassword = model.NewPassword,
        }, cancellationToken);

        if (!outcome.Succeeded)
        {
            foreach (var err in outcome.Errors) ModelState.AddModelError(string.Empty, err);
            return View(model);
        }

        TempData["AccountMessage"] = "Password reset. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            ViewBag.Message = "Invalid confirmation link.";
            return View();
        }

        var outcome = await _api.ConfirmEmailAsync(new ConfirmEmailRequestDto
        {
            UserId = userId,
            ConfirmationToken = token,
        }, cancellationToken);

        ViewBag.Message = outcome.Succeeded
            ? "Email confirmed. You can now sign in."
            : "Email confirmation failed: " + string.Join("; ", outcome.Errors);
        return View();
    }

    private async Task SignInAsync(UserSummaryDto user, StoredTokens tokens)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(AuthClaimTypes.AccessToken, tokens.AccessToken),
            new(AuthClaimTypes.RefreshToken, tokens.RefreshToken),
            new(AuthClaimTypes.AccessTokenExpiresUtc, tokens.ExpiresAtUtc.ToString("O")),
            new(AuthClaimTypes.MustChangePassword, user.MustChangePassword.ToString()),
        };
        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(_settings.CookieLifetime),
        });
    }

    private IActionResult SafeLocalRedirect(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}
