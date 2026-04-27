using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.WebPortal.Controllers;

/// <summary>
/// Same-origin bridge for the settings.js auto-load + auto-save flow.
/// Forwards the opaque settings blob through the bearer-attached
/// HttpClient to the API. The blob's schema is owned by the JS layer;
/// this controller only proxies bytes.
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
public sealed class SettingsController : ControllerBase
{
    private readonly IUserSettingsApiService _api;

    public SettingsController(IUserSettingsApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken = default)
    {
        var settings = await _api.GetMineAsync(cancellationToken);
        return settings is null ? StatusCode(502) : Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> SaveMine([FromBody] SaveUserSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var settings = await _api.SaveMineAsync(request.Json ?? string.Empty, cancellationToken);
        return settings is null ? StatusCode(502) : Ok(settings);
    }
}
