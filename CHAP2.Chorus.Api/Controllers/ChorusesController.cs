using Microsoft.AspNetCore.Mvc;
using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using CHAP2.Domain.Constants;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Helpers;
using CHAP2.Chorus.Api.Configuration;
using CHAP2.Chorus.Api.Requests;
using CHAP2.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ChorusesController : ChapControllerAbstractBase
{
    private readonly IChorusQueryService _chorusQueryService;
    private readonly IChorusCommandService _chorusCommandService;
    private readonly SearchSettings _searchSettings;
    
    public ChorusesController(
        ILogger<ChorusesController> logger, 
        IChorusQueryService chorusQueryService,
        IChorusCommandService chorusCommandService,
        IOptions<SearchSettings> searchSettings) 
        : base(logger)
    {
        _chorusQueryService = chorusQueryService;
        _chorusCommandService = chorusCommandService;
        _searchSettings = searchSettings.Value;
    }

    [HttpPost]
    public async Task<IActionResult> AddChorus([FromBody] CreateChorusRequest request, CancellationToken cancellationToken = default)
    {
        LogAction("AddChorus", new { request.Name });
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var chorus = await _chorusCommandService.CreateChorusAsync(
                InputSanitizer.SanitizeName(request.Name),
                InputSanitizer.SanitizeChorusText(request.ChorusText),
                request.Key,
                request.Type,
                request.TimeSignature,
                cancellationToken);

            return CreatedAtAction(nameof(GetChorusById), new { id = chorus.Id }, chorus);
        }
        catch (ChorusAlreadyExistsException ex)
        {
            return Conflict(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllChoruses(CancellationToken cancellationToken = default)
    {
        LogAction("GetAllChoruses");
        
        var choruses = await _chorusQueryService.GetAllChorusesAsync(cancellationToken);
        return Ok(choruses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetChorusById(Guid id, CancellationToken cancellationToken = default)
    {
        LogAction("GetChorusById", new { id });
        
        try
        {
            var chorus = await _chorusQueryService.GetChorusByIdAsync(id, cancellationToken);
            return Ok(chorus);
        }
        catch (ChorusNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchChoruses(
        [FromQuery] string? q = null,
        [FromQuery] SearchMode searchMode = SearchMode.Contains,
        [FromQuery] string? searchIn = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search query 'q' is required");
        }

        // Sanitize search query
        q = InputSanitizer.SanitizeSearchQuery(q);

        if (searchMode == SearchMode.Contains && !string.IsNullOrEmpty(_searchSettings.DefaultSearchMode))
        {
            if (System.Enum.TryParse<SearchMode>(_searchSettings.DefaultSearchMode, true, out var defaultMode))
            {
                searchMode = defaultMode;
            }
        }

        searchIn ??= _searchSettings.DefaultSearchScope;

        LogAction("SearchChoruses", new { q, searchMode, searchIn });

        try
        {
            var searchScope = searchIn?.ToLowerInvariant() switch
            {
                SearchScopeConstants.Name => SearchScope.Name,
                SearchScopeConstants.Text => SearchScope.Text,
                SearchScopeConstants.Key => SearchScope.Key,
                SearchScopeConstants.All or _ => SearchScope.All
            };

            var results = await _chorusQueryService.SearchChorusesAsync(q, searchMode, searchScope, cancellationToken);

            if (results.Count > _searchSettings.MaxResults)
            {
                results = results.Take(_searchSettings.MaxResults).ToList();
            }

            return Ok(new
            {
                query = q,
                searchMode = searchMode.ToString(),
                searchIn = searchIn,
                count = results.Count,
                maxResults = _searchSettings.MaxResults,
                results = results
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetChorusByName(string name, CancellationToken cancellationToken = default)
    {
        LogAction("GetChorusByName", new { name });
        
        try
        {
            var chorus = await _chorusQueryService.GetChorusByNameAsync(name, cancellationToken);
            return Ok(chorus);
        }
        catch (ChorusNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChorus(Guid id, [FromBody] UpdateChorusRequest request, CancellationToken cancellationToken = default)
    {
        LogAction("UpdateChorus", new { id, request.Name });
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        try
        {
            var updatedChorus = await _chorusCommandService.UpdateChorusAsync(
                id,
                InputSanitizer.SanitizeName(request.Name),
                InputSanitizer.SanitizeChorusText(request.ChorusText),
                request.Key,
                request.Type,
                request.TimeSignature,
                cancellationToken);

            return Ok(updatedChorus);
        }
        catch (ChorusNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ChorusAlreadyExistsException ex)
        {
            return Conflict(ex.Message);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChorus(Guid id, CancellationToken cancellationToken = default)
    {
        LogAction("DeleteChorus", new { id });
        
        try
        {
            await _chorusCommandService.DeleteChorusAsync(id, cancellationToken);
            return NoContent();
        }
        catch (ChorusNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
} 