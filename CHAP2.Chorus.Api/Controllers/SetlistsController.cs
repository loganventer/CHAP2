using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using CHAP2.Domain.ValueObjects;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using ChorusEntity = CHAP2.Domain.Entities.Chorus;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("setlists")]
public class SetlistsController : ChapControllerAbstractBase
{
    private readonly ISetlistQueryService _query;
    private readonly ISetlistCommandService _command;
    private readonly IChorusReadRepository _chorusReadRepository;

    public SetlistsController(
        ILogger<SetlistsController> logger,
        ISetlistQueryService query,
        ISetlistCommandService command,
        IChorusReadRepository chorusReadRepository) : base(logger)
    {
        _query = query;
        _command = command;
        _chorusReadRepository = chorusReadRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var setlists = await _query.GetMineAsync(cancellationToken);
        var summaries = setlists.Select(MapToSummary).ToList();
        return Ok(summaries);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var setlist = await _query.GetByIdAsync(id, cancellationToken);
            if (setlist is null) return NotFound();
            return Ok(await MapToDtoAsync(setlist, cancellationToken));
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
            return CreatedAtAction(nameof(GetById), new { id = setlist.Id }, await MapToDtoAsync(setlist, cancellationToken));
        }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Atomic upsert by name for the current user. The full item array
    /// replaces whatever's there (or creates a fresh setlist if no
    /// same-named one exists). Used by the Portal's explicit Save flow.
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveSetlistRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var payloads = (request.Items ?? Array.Empty<SetlistItemPayloadDto>()).Select(MapToPayload).ToList();
            var setlist = await _command.SaveByNameAsync(request.Name, payloads, cancellationToken);
            return Ok(await MapToDtoAsync(setlist, cancellationToken));
        }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// The current user's auto-saved working setlist. Returns 204 if the
    /// user hasn't accumulated any items yet.
    /// </summary>
    [HttpGet("working")]
    public async Task<IActionResult> GetWorkingDraft(CancellationToken cancellationToken = default)
    {
        var draft = await _query.GetWorkingDraftAsync(cancellationToken);
        if (draft is null) return NoContent();
        return Ok(await MapToDtoAsync(draft, cancellationToken));
    }

    /// <summary>
    /// Replaces the current user's working setlist atomically. Body is just
    /// the items array; name is server-managed.
    /// </summary>
    [HttpPut("working")]
    public async Task<IActionResult> SaveWorkingDraft([FromBody] SaveWorkingDraftRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payloads = (request.Items ?? Array.Empty<SetlistItemPayloadDto>()).Select(MapToPayload).ToList();
            var draft = await _command.SaveWorkingDraftAsync(payloads, cancellationToken);
            return Ok(await MapToDtoAsync(draft, cancellationToken));
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
            return Ok(await MapToDtoAsync(setlist, cancellationToken));
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
            return Ok(await MapToDtoAsync(setlist, cancellationToken));
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
            return Ok(await MapToDtoAsync(setlist, cancellationToken));
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
            return Ok(await MapToDtoAsync(setlist, cancellationToken));
        }
        catch (SetlistNotFoundException) { return NotFound(); }
        catch (SetlistAccessDeniedException) { return Forbid(); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    private static SetlistSummaryDto MapToSummary(Setlist setlist) => new()
    {
        Id = setlist.Id,
        Name = setlist.Name,
        ItemCount = setlist.Items.Count,
        CreatedAt = setlist.CreatedAt,
        UpdatedAt = setlist.UpdatedAt,
    };

    private async Task<SetlistDto> MapToDtoAsync(Setlist setlist, CancellationToken cancellationToken)
    {
        var chorusIds = setlist.Items
            .Where(i => i.Kind == SetlistItemKind.Chorus && i.ChorusId.HasValue)
            .Select(i => i.ChorusId!.Value)
            .Distinct()
            .ToList();

        var chorusMap = chorusIds.Count == 0
            ? new Dictionary<Guid, ChorusEntity>()
            : (await _chorusReadRepository.GetByIdsAsync(chorusIds, cancellationToken)).ToDictionary(c => c.Id);

        return new SetlistDto
        {
            Id = setlist.Id,
            OwnerId = setlist.OwnerId,
            Name = setlist.Name,
            CreatedAt = setlist.CreatedAt,
            UpdatedAt = setlist.UpdatedAt,
            Items = setlist.Items.Select(i => MapItem(i, chorusMap)).ToArray(),
        };
    }

    private static SetlistItemDto MapItem(SetlistItem item, IReadOnlyDictionary<Guid, ChorusEntity> chorusMap)
    {
        if (item.Kind == SetlistItemKind.Verse)
        {
            return new SetlistItemDto
            {
                Id = item.Id,
                Position = item.Position,
                Kind = "verse",
                BookId = item.BookId,
                BookName = item.BookName,
                Chapter = item.Chapter,
                Verse = item.Verse,
                Text = item.VerseText,
                Ref = item.VerseRef,
            };
        }

        var chorus = item.ChorusId.HasValue && chorusMap.TryGetValue(item.ChorusId.Value, out var c) ? c : null;
        return new SetlistItemDto
        {
            Id = item.Id,
            Position = item.Position,
            Kind = "chorus",
            ChorusId = item.ChorusId,
            ChorusName = chorus?.Name,
            ChorusKey = chorus?.Key.ToString(),
            ChorusType = chorus?.Type.ToString(),
            ChorusTimeSignature = chorus?.TimeSignature.ToString(),
        };
    }

    private static SetlistItemPayload MapToPayload(SetlistItemPayloadDto dto)
    {
        if (string.Equals(dto.Kind, "verse", StringComparison.OrdinalIgnoreCase))
        {
            return SetlistItemPayload.ForVerse(
                dto.BookId ?? string.Empty,
                dto.BookName ?? string.Empty,
                dto.Chapter ?? 0,
                dto.Verse ?? 0,
                dto.Text ?? string.Empty,
                dto.Ref ?? string.Empty);
        }
        return SetlistItemPayload.ForChorus(dto.ChorusId ?? throw new DomainException("Chorus item missing ChorusId."));
    }
}
