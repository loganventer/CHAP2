using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("me/preferences")]
public class UserPreferencesController : ChapControllerAbstractBase
{
    private readonly IUserPreferencesService _service;

    public UserPreferencesController(
        ILogger<UserPreferencesController> logger,
        IUserPreferencesService service) : base(logger)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var prefs = await _service.GetMineAsync(cancellationToken);
        return Ok(MapToDto(prefs));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateUserPreferencesRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!Enum.TryParse<Theme>(request.Theme, ignoreCase: true, out var theme))
            return BadRequest($"Unknown theme '{request.Theme}'.");
        if (!Enum.TryParse<SearchScope>(request.DefaultSearchScope, ignoreCase: true, out var scope))
            return BadRequest($"Unknown search scope '{request.DefaultSearchScope}'.");
        if (!Enum.TryParse<Language>(request.Language, ignoreCase: true, out var language))
            return BadRequest($"Unknown language '{request.Language}'.");

        var prefs = await _service.UpdateMineAsync(theme, scope, language, cancellationToken);
        return Ok(MapToDto(prefs));
    }

    private static UserPreferencesDto MapToDto(UserPreferences prefs) => new()
    {
        UserId = prefs.UserId,
        Theme = prefs.Theme.ToString(),
        DefaultSearchScope = prefs.DefaultSearchScope.ToString(),
        Language = prefs.Language.ToString(),
        UpdatedAt = prefs.UpdatedAt,
    };
}
