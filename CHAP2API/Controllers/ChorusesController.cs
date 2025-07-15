using CHAP2API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CHAP2.Common.Models;
using CHAP2.Common.Interfaces;
using CHAP2.Common.Services;
using CHAP2API.Configuration;
using Microsoft.Extensions.Options;

namespace CHAP2API.Controllers;

/// <summary>
/// Choruses controller
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChorusesController : ChapControllerAbstractBase
{
    private readonly IChorusResource _chorusResource;
    private readonly ISearchService _searchService;
    private readonly SearchSettings _searchSettings;
    
    public ChorusesController(
        ILogger<ChorusesController> logger, 
        IChorusResource chorusResource, 
        ISearchService searchService,
        IOptions<SearchSettings> searchSettings) 
        : base(logger)
    {
        _chorusResource = chorusResource;
        _searchService = searchService;
        _searchSettings = searchSettings.Value;
    }

    /// <summary>
    /// Add a new chorus
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddChorus([FromBody] Chorus chorus, CancellationToken cancellationToken = default)
    {
        LogAction("AddChorus", new { chorus.Name });
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if a chorus with the same name already exists
        if (await _chorusResource.ChorusExistsAsync(chorus.Name, cancellationToken))
        {
            return Conflict($"A chorus with the name '{chorus.Name}' already exists.");
        }

        await _chorusResource.AddChorusAsync(chorus, cancellationToken);
        return CreatedAtAction(nameof(GetChorusById), new { id = chorus.Id }, chorus);
    }

    /// <summary>
    /// Get all choruses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllChoruses(CancellationToken cancellationToken = default)
    {
        LogAction("GetAllChoruses");
        
        var choruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        return Ok(choruses);
    }

    /// <summary>
    /// Get a chorus by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChorusById(Guid id, CancellationToken cancellationToken = default)
    {
        LogAction("GetChorusById", new { id });
        
        var chorus = await _chorusResource.GetChorusByIdAsync(id, cancellationToken);
        if (chorus == null)
            return NotFound();
        return Ok(chorus);
    }

    /// <summary>
    /// Search choruses with comprehensive search capabilities
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchChoruses(
        [FromQuery] string? q = null,
        [FromQuery] SearchMode searchMode = SearchMode.Contains,
        [FromQuery] string? searchIn = null, // "name", "text", or "all"
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search query 'q' is required");
        }

        // Use configured defaults if not provided
        if (searchMode == SearchMode.Contains && !string.IsNullOrEmpty(_searchSettings.DefaultSearchMode))
        {
            if (System.Enum.TryParse<SearchMode>(_searchSettings.DefaultSearchMode, true, out var defaultMode))
            {
                searchMode = defaultMode;
            }
        }

        searchIn ??= _searchSettings.DefaultSearchScope;

        LogAction("SearchChoruses", new { q, searchMode, searchIn });

        IReadOnlyList<Chorus> results = searchIn?.ToLowerInvariant() switch
        {
            "name" => await _searchService.SearchByNameAsync(q, searchMode, cancellationToken),
            "text" => await _searchService.SearchByTextAsync(q, searchMode, cancellationToken),
            "key" => await _searchService.SearchByKeyAsync(q, searchMode, cancellationToken),
            "all" or _ => await _searchService.SearchAllAsync(q, searchMode, cancellationToken)
        };

        // Apply max results limit from configuration
        if (results.Count > _searchSettings.MaxSearchResults)
        {
            results = results.Take(_searchSettings.MaxSearchResults).ToList();
        }

        return Ok(new
        {
            query = q,
            searchMode = searchMode.ToString(),
            searchIn = searchIn,
            count = results.Count,
            maxResults = _searchSettings.MaxSearchResults,
            results = results
        });
    }

    /// <summary>
    /// Get a chorus by exact name match (case-insensitive)
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetChorusByName(string name, CancellationToken cancellationToken = default)
    {
        LogAction("GetChorusByName", new { name });
        var results = await _searchService.SearchByNameAsync(name, SearchMode.Exact, cancellationToken);
        var chorus = results.FirstOrDefault();
        if (chorus == null)
            return NotFound();
        return Ok(chorus);
    }

    /// <summary>
    /// Update a chorus
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChorus(Guid id, [FromBody] Chorus chorus, CancellationToken cancellationToken = default)
    {
        LogAction("UpdateChorus", new { id, chorus.Name });
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        if (id != chorus.Id)
            return BadRequest("ID mismatch");
            
        var existingChorus = await _chorusResource.GetChorusByIdAsync(id, cancellationToken);
        if (existingChorus == null)
            return NotFound();

        // Check if the new name conflicts with another chorus (excluding this one)
        var choruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        var nameConflict = choruses.Any(c => 
            c.Id != id && 
            string.Equals(c.Name, chorus.Name, StringComparison.OrdinalIgnoreCase));
            
        if (nameConflict)
        {
            return Conflict($"A chorus with the name '{chorus.Name}' already exists.");
        }
            
        await _chorusResource.UpdateChorusAsync(chorus, cancellationToken);
        return Ok(chorus);
    }


} 