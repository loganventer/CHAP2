using System.Security.Claims;
using CHAP2.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CHAP2.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string UserId => Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("No authenticated user on this request.");

    public string UserName => Principal?.Identity?.Name
        ?? throw new InvalidOperationException("No authenticated user on this request.");

    public IReadOnlyList<string> Roles => Principal?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToArray() ?? Array.Empty<string>();

    public bool IsAdmin => Roles.Contains(RoleNames.Admin, StringComparer.Ordinal);
}
