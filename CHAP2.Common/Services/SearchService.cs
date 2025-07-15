using CHAP2.Common.Models;
using CHAP2.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Common.Services;

public class SearchService : ISearchService
{
    private readonly IChorusResource _chorusResource;
    private readonly IRegexHelperService _regexHelper;
    private readonly ILogger<SearchService> _logger;

    public SearchService(IChorusResource chorusResource, IRegexHelperService regexHelper, ILogger<SearchService> logger)
    {
        _chorusResource = chorusResource;
        _regexHelper = regexHelper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Chorus>> SearchByNameAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        var results = new List<Chorus>();

        foreach (var chorus in allChoruses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (MatchesSearch(chorus.Name, searchTerm, searchMode))
            {
                results.Add(chorus);
            }
        }

        _logger.LogInformation("SearchByName found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results;
    }

    public async Task<IReadOnlyList<Chorus>> SearchByTextAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        var results = new List<Chorus>();

        foreach (var chorus in allChoruses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (MatchesSearch(chorus.ChorusText, searchTerm, searchMode))
            {
                results.Add(chorus);
            }
        }

        _logger.LogInformation("SearchByText found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results;
    }

    public async Task<IReadOnlyList<Chorus>> SearchByKeyAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        var results = new List<Chorus>();

        foreach (var chorus in allChoruses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Convert the key to string for searching
            var keyString = chorus.Key.ToString();
            if (MatchesSearch(keyString, searchTerm, searchMode))
            {
                results.Add(chorus);
            }
        }

        _logger.LogInformation("SearchByKey found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results;
    }

    public async Task<IReadOnlyList<Chorus>> SearchAllAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        var results = new HashSet<Chorus>(); // Use HashSet to avoid duplicates

        foreach (var chorus in allChoruses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (MatchesSearch(chorus.Name, searchTerm, searchMode) || 
                MatchesSearch(chorus.ChorusText, searchTerm, searchMode))
            {
                results.Add(chorus);
            }
        }

        _logger.LogInformation("SearchAll found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results.ToList();
    }

    private bool MatchesSearch(string text, string searchTerm, SearchMode searchMode)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return searchMode switch
        {
            SearchMode.Exact => string.Equals(text, searchTerm, StringComparison.OrdinalIgnoreCase),
            SearchMode.Contains => text.Contains(searchTerm, StringComparison.OrdinalIgnoreCase),
            SearchMode.Regex => _regexHelper.IsRegexMatch(text, searchTerm),
            _ => false
        };
    }
} 