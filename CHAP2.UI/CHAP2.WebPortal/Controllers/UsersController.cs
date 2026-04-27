using CHAP2.Infrastructure.Identity;
using CHAP2.WebPortal.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.WebPortal.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public sealed class UsersController : Controller
{
    private readonly IUserAdminApiService _api;

    public UsersController(IUserAdminApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var users = await _api.ListAsync(cancellationToken);
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string id, string role, CancellationToken cancellationToken = default)
    {
        await _api.AssignRoleAsync(id, role, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeRole(string id, string role, CancellationToken cancellationToken = default)
    {
        await _api.RevokeRoleAsync(id, role, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        await _api.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
