using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.WebPortal.Controllers;

/// <summary>
/// Same-origin bridge for the in-page setlist UI: the JS POSTs JSON to
/// these actions, which forward to the API with the user's bearer token
/// (via the configured BearerTokenHandler on CHAP2API HttpClient).
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
public sealed class SetlistsController : ControllerBase
{
    private readonly ISetlistApiService _api;

    public SetlistsController(ISetlistApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken = default)
    {
        var summaries = await _api.ListMineAsync(cancellationToken);
        return Ok(summaries);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken = default)
    {
        var setlist = await _api.GetByIdAsync(id, cancellationToken);
        return setlist is null ? NotFound() : Ok(setlist);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveSetlistRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var setlist = await _api.SaveByNameAsync(request.Name, request.Items, cancellationToken);
        return setlist is null ? StatusCode(502) : Ok(setlist);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await _api.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : StatusCode(502);
    }

    [HttpGet]
    public async Task<IActionResult> Working(CancellationToken cancellationToken = default)
    {
        var draft = await _api.GetWorkingDraftAsync(cancellationToken);
        if (draft is null) return NoContent();
        return Ok(draft);
    }

    [HttpPut]
    public async Task<IActionResult> SaveWorking([FromBody] SaveWorkingDraftRequestDto request, CancellationToken cancellationToken = default)
    {
        var draft = await _api.SaveWorkingDraftAsync(request.Items, cancellationToken);
        return draft is null ? StatusCode(502) : Ok(draft);
    }
}
