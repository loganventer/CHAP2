using System.Security.Claims;
using CHAP2.Infrastructure.Identity;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CHAP2.Chorus.Api.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapChap2Identity(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/identity");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous();
        group.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous();
        group.MapPost("/confirm-email", ConfirmEmailAsync).AllowAnonymous();

        group.MapGet("/me", GetMeAsync).RequireAuthorization();
        group.MapPost("/change-password", ChangePasswordAsync).RequireAuthorization();

        return routes;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequestDto request,
        UserManager<ApplicationUser> userManager,
        IEmailSender<ApplicationUser> emailSender)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded) return Validation(create);

        var addRole = await userManager.AddToRoleAsync(user, RoleNames.User);
        if (!addRole.Succeeded) return Validation(addRole);

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        try
        {
            await emailSender.SendConfirmationLinkAsync(user, request.Email, confirmationToken);
        }
        catch
        {
            // Do not fail registration on email failure; user can request resend later.
        }

        return Results.Created($"/identity/users/{user.Id}", new { user.Id, user.UserName, user.Email });
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequestDto request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null) return Results.Unauthorized();

        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut) return Results.Problem(statusCode: StatusCodes.Status423Locked, detail: "Account is locked. Try again later.");
        if (!result.Succeeded) return Results.Unauthorized();

        return TypedResults.Empty;
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequestDto request,
        SignInManager<ApplicationUser> signInManager,
        IOptionsMonitor<BearerTokenOptions> bearerOptions,
        TimeProvider timeProvider)
    {
        var protector = bearerOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var ticket = protector.Unprotect(request.RefreshToken);

        if (ticket?.Properties?.ExpiresUtc is not { } expiresUtc
            || timeProvider.GetUtcNow() >= expiresUtc
            || await signInManager.ValidateSecurityStampAsync(ticket.Principal) is not ApplicationUser user)
        {
            return TypedResults.Challenge();
        }

        var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
        return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return Results.Ok(new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray(),
            CreatedAtUtc = user.CreatedAtUtc,
            MustChangePassword = user.MustChangePassword,
        });
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequestDto request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return Validation(result);

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await userManager.UpdateAsync(user);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequestDto request,
        UserManager<ApplicationUser> userManager,
        IEmailSender<ApplicationUser> emailSender)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            try
            {
                await emailSender.SendPasswordResetLinkAsync(user, request.Email, token);
            }
            catch
            {
                // Swallow to avoid revealing email-existence via differential errors.
            }
        }
        return Results.NoContent();
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequestDto request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return Results.BadRequest("Invalid reset request.");

        var result = await userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
        if (!result.Succeeded) return Validation(result);
        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmEmailAsync(
        [FromBody] ConfirmEmailRequestDto request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return Results.BadRequest("Invalid confirmation request.");

        var result = await userManager.ConfirmEmailAsync(user, request.ConfirmationToken);
        if (!result.Succeeded) return Validation(result);
        return Results.NoContent();
    }

    private static IResult Validation(IdentityResult result) =>
        Results.ValidationProblem(result.Errors.GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
}
