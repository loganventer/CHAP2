using CHAP2.Infrastructure.Identity;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = RoleNames.Admin)]
public class UsersController : ChapControllerAbstractBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        ILogger<UsersController> logger,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager) : base(logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);

        var summaries = new List<UserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            summaries.Add(await ToSummaryAsync(user));
        }
        return Ok(summaries);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(await ToSummaryAsync(user));
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await _roleManager.RoleExistsAsync(request.Role)) return BadRequest($"Unknown role '{request.Role}'.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, request.Role))
            return Ok(await ToSummaryAsync(user));

        var result = await _userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded) return BadRequest(Describe(result));
        return Ok(await ToSummaryAsync(user));
    }

    [HttpDelete("{id}/roles/{role}")]
    public async Task<IActionResult> RevokeRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!await _userManager.IsInRoleAsync(user, role))
            return Ok(await ToSummaryAsync(user));

        if (string.Equals(role, RoleNames.Admin, StringComparison.Ordinal))
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            if (admins.Count <= 1)
                return BadRequest("Cannot remove the last Admin.");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded) return BadRequest(Describe(result));
        return Ok(await ToSummaryAsync(user));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            if (admins.Count <= 1)
                return BadRequest("Cannot delete the last Admin.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(Describe(result));
        return NoContent();
    }

    private async Task<UserSummaryDto> ToSummaryAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray(),
            CreatedAtUtc = user.CreatedAtUtc,
            MustChangePassword = user.MustChangePassword,
        };
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}"));
}
