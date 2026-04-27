using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Exceptions;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("setlists")]
public class SetlistsController : ChapControllerAbstractBase
{
    private readonly ISetlistQueryService _query;
    private readonly ISetlistCommandService _command;

    public SetlistsController(
        ILogger<SetlistsController> logger,
        ISetlistQueryService query,
        ISetlistCommandService command) : base(logger)
    {
        _query = query;
        _command = command;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var setlists = await _query.GetMineAsync(cancellationToken);
        return Ok(setlists.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var setlist = await _query.GetByIdAsync(id, cancellationToken);
            return setlist is null ? NotFound() : Ok(MapToDto(setlist));
        }
        catch (SetlistAccessDeniedException) { return Forbid(); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSetlistRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var setlist = await _command.CreateMineAsync(request.Name, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = setlist.Id }, MapToDto(setlist));
        }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameSetlistRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var setlist = await _command.RenameAsync(id, request.Name, cancellationToken);
            return Ok(MapToDto(setlist));
        }
        catch (SetlistNotFoundException) { return NotFound(); }
        catch (SetlistAccessDeniedException) { return Forbid(); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _command.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (SetlistNotFoundException) { return NotFound(); }
        catch (SetlistAccessDeniedException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AppendItem(Guid id, [FromBody] AppendChorusRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var setlist = await _command.AppendChorusAsync(id, request.ChorusId, cancellationToken);
            return Ok(MapToDto(setlist));
        }
        catch (SetlistNotFoundException) { return NotFound(); }
        catch (SetlistAccessDeniedException) { return Forbid(); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var setlist = await _command.RemoveItemAsync(id, itemId, cancellationToken);
            return Ok(MapToDto(setlist));
        }
        catch (SetlistNotFoundException) { return NotFound(); }
        catch (SetlistAccessDeniedException) { return Forbid(); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("{id:guid}/reorder")]
    public async Task<IActionResult> Reorder(Guid id, [FromBody] ReorderSetlistRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var setlist = await _command.ReorderAsync(id, request.ItemIdsInOrder, cancellationToken);
            return Ok(MapToDto(setlist));
        }
        catch (SetlistNotFoundException) { return NotFound(); }
        catch (SetlistAccessDeniedException) { return Forbid(); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    private static SetlistDto MapToDto(Setlist setlist) => new()
    {
        Id = setlist.Id,
        OwnerId = setlist.OwnerId,
        Name = setlist.Name,
        CreatedAt = setlist.CreatedAt,
        UpdatedAt = setlist.UpdatedAt,
        Items = setlist.Items.Select(i => new SetlistItemDto
        {
            Id = i.Id,
            ChorusId = i.ChorusId,
            Position = i.Position,
        }).ToArray(),
    };
}
