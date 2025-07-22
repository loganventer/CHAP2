using Microsoft.AspNetCore.Mvc;
using CHAP2.Application.Interfaces;
using CHAP2.Domain.Enums;
using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchApiRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            var searchRequest = new SearchRequest(
                Query: request.Query.Trim(),
                Mode: ParseSearchMode(request.SearchMode),
                Scope: ParseSearchScope(request.SearchScope),
                MaxResults: request.MaxResults ?? 50
            );

            var result = await _searchService.SearchAsync(searchRequest);

            if (!string.IsNullOrEmpty(result.Error))
            {
                return StatusCode(500, new { error = result.Error });
            }

            var response = new
            {
                results = result.Results.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    key = (int)c.Key,
                    type = (int)c.Type,
                    timeSignature = (int)c.TimeSignature,
                    chorusText = c.ChorusText,
                    createdAt = c.CreatedAt
                }),
                totalCount = result.TotalCount,
                metadata = result.Metadata
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for query: {Query}", request.Query);
            return StatusCode(500, new { error = "Search failed. Please try again." });
        }
    }

    [HttpPost("ai-search")]
    public async Task<IActionResult> AiSearch([FromBody] SearchApiRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            var searchRequest = new SearchRequest(
                Query: request.Query.Trim(),
                Mode: ParseSearchMode(request.SearchMode),
                Scope: ParseSearchScope(request.SearchScope),
                MaxResults: request.MaxResults ?? 50,
                UseAi: true
            );

            var result = await _searchService.SearchWithAiAsync(searchRequest);

            if (!string.IsNullOrEmpty(result.Error))
            {
                return StatusCode(500, new { error = result.Error });
            }

            var response = new
            {
                results = result.Results.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    key = (int)c.Key,
                    type = (int)c.Type,
                    timeSignature = (int)c.TimeSignature,
                    chorusText = c.ChorusText,
                    createdAt = c.CreatedAt
                }),
                totalCount = result.TotalCount,
                metadata = result.Metadata
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI search for query: {Query}", request.Query);
            return StatusCode(500, new { error = "AI search failed. Please try again." });
        }
    }

    private static SearchMode ParseSearchMode(string? mode)
    {
        return mode?.ToLowerInvariant() switch
        {
            "exact" => SearchMode.Exact,
            "regex" => SearchMode.Regex,
            _ => SearchMode.Contains
        };
    }

    private static SearchScope ParseSearchScope(string? scope)
    {
        return scope?.ToLowerInvariant() switch
        {
            "name" => SearchScope.Name,
            "text" => SearchScope.Text,
            "key" => SearchScope.Key,
            _ => SearchScope.All
        };
    }
}

public record SearchApiRequest(
    string Query,
    string? SearchMode = "Contains",
    string? SearchScope = "All",
    int? MaxResults = 50
); 