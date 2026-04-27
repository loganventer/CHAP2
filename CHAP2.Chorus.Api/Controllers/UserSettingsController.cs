using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("me/settings")]
public class UserSettingsController : ChapControllerAbstractBase
{
    private readonly IUserSettingsService _service;

    public UserSettingsController(
        ILogger<UserSettingsController> logger,
        IUserSettingsService service) : base(logger)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var settings = await _service.GetMineAsync(cancellationToken);
        return Ok(MapToDto(settings));
    }

    [HttpPut]
    public async Task<IActionResult> SaveMine([FromBody] SaveUserSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var settings = await _service.SaveMineAsync(request.Json ?? string.Empty, cancellationToken);
        return Ok(MapToDto(settings));
    }

    private static UserSettingsDto MapToDto(UserSettings settings) => new()
    {
        UserId = settings.UserId,
        Json = settings.Json,
        UpdatedAt = settings.UpdatedAt,
    };
}
