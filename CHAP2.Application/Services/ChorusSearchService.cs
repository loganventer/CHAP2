using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace CHAP2.Application.Services;

public class ChorusSearchService : ISearchService
{
    private readonly IChorusRepository _chorusRepository;
    private readonly ILogger<ChorusSearchService> _logger;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
    private const string AllChorusesCacheKey = "AllChoruses";
    private const int CacheDurationMinutes = 15;

    public ChorusSearchService(
        IChorusRepository chorusRepository,
        ILogger<ChorusSearchService> logger,
        IMemoryCache cache)
    {
        _chorusRepository = chorusRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<Chorus>> SearchByNameAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await GetCachedChorusesAsync(cancellationToken);
        var results = new List<Chorus>();

        // Use parallel processing for large datasets
        if (allChoruses.Count > 100)
        {
            var parallelResults = new ConcurrentBag<Chorus>();
            await Parallel.ForEachAsync(allChoruses, cancellationToken, (chorus, ct) =>
            {
                if (MatchesSearch(chorus.Name, searchTerm, searchMode))
                {
                    parallelResults.Add(chorus);
                }
                return ValueTask.CompletedTask;
            });
            results.AddRange(parallelResults);
        }
        else
        {
            foreach (var chorus in allChoruses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (MatchesSearch(chorus.Name, searchTerm, searchMode))
                {
                    results.Add(chorus);
                }
            }
        }

        _logger.LogInformation("SearchByName found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results;
    }

    public async Task<IReadOnlyList<Chorus>> SearchByTextAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await GetCachedChorusesAsync(cancellationToken);
        var results = new List<Chorus>();

        // Use parallel processing for large datasets
        if (allChoruses.Count > 100)
        {
            var parallelResults = new ConcurrentBag<Chorus>();
            await Parallel.ForEachAsync(allChoruses, cancellationToken, (chorus, ct) =>
            {
                if (MatchesSearch(chorus.ChorusText, searchTerm, searchMode))
                {
                    parallelResults.Add(chorus);
                }
                return ValueTask.CompletedTask;
            });
            results.AddRange(parallelResults);
        }
        else
        {
            foreach (var chorus in allChoruses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (MatchesSearch(chorus.ChorusText, searchTerm, searchMode))
                {
                    results.Add(chorus);
                }
            }
        }

        _logger.LogInformation("SearchByText found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results;
    }

    public async Task<IReadOnlyList<Chorus>> SearchByKeyAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var allChoruses = await GetCachedChorusesAsync(cancellationToken);
        var results = new List<Chorus>();

        foreach (var chorus in allChoruses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
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
        
        var allChoruses = await GetCachedChorusesAsync(cancellationToken);
        var results = new HashSet<Chorus>(); // Use HashSet to avoid duplicates

        // Use parallel processing for large datasets
        if (allChoruses.Count > 100)
        {
            var parallelResults = new ConcurrentBag<Chorus>();
            await Parallel.ForEachAsync(allChoruses, cancellationToken, (chorus, ct) =>
            {
                if (MatchesSearch(chorus.Name, searchTerm, searchMode) || 
                    MatchesSearch(chorus.ChorusText, searchTerm, searchMode))
                {
                    parallelResults.Add(chorus);
                }
                return ValueTask.CompletedTask;
            });
            foreach (var result in parallelResults)
            {
                results.Add(result);
            }
        }
        else
        {
            foreach (var chorus in allChoruses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (MatchesSearch(chorus.Name, searchTerm, searchMode) || 
                    MatchesSearch(chorus.ChorusText, searchTerm, searchMode))
                {
                    results.Add(chorus);
                }
            }
        }

        _logger.LogInformation("SearchAll found {Count} results for term '{SearchTerm}' with mode {SearchMode}", 
            results.Count, searchTerm, searchMode);
        
        return results.ToList();
    }

    private async Task<IReadOnlyList<Chorus>> GetCachedChorusesAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(AllChorusesCacheKey, out IReadOnlyList<Chorus>? cachedChoruses))
        {
            return cachedChoruses!;
        }

        await _cacheSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check pattern
            if (_cache.TryGetValue(AllChorusesCacheKey, out cachedChoruses))
            {
                return cachedChoruses!;
            }

            var choruses = await _chorusRepository.GetAllAsync(cancellationToken);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };
            
            _cache.Set(AllChorusesCacheKey, choruses, cacheOptions);
            return choruses;
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    public void InvalidateCache()
    {
        _cache.Remove(AllChorusesCacheKey);
    }

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                _logger.LogWarning("Search query is null or empty");
                return new SearchResult(new List<Chorus>(), 0);
            }

            _logger.LogInformation("Performing search with query: {Query}, mode: {Mode}, scope: {Scope}", 
                request.Query, request.Mode, request.Scope);

            var results = request.Scope switch
            {
                SearchScope.Name => await SearchByNameAsync(request.Query, request.Mode, cancellationToken),
                SearchScope.Text => await SearchByTextAsync(request.Query, request.Mode, cancellationToken),
                SearchScope.Key => await SearchByKeyAsync(request.Query, request.Mode, cancellationToken),
                SearchScope.All => await SearchAllAsync(request.Query, request.Mode, cancellationToken),
                _ => await SearchAllAsync(request.Query, request.Mode, cancellationToken)
            };

            var limitedResults = results.Take(request.MaxResults).ToList();

            _logger.LogInformation("Search completed. Found {TotalCount} results, returning {LimitedCount}", 
                results.Count, limitedResults.Count);

            return new SearchResult(
                limitedResults,
                results.Count,
                Metadata: new Dictionary<string, object>
                {
                    ["query"] = request.Query,
                    ["mode"] = request.Mode.ToString(),
                    ["scope"] = request.Scope.ToString(),
                    ["maxResults"] = request.MaxResults,
                    ["totalFound"] = results.Count,
                    ["returnedCount"] = limitedResults.Count
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search with query: {Query}", request.Query);
            return new SearchResult(
                new List<Chorus>(), 
                0, 
                Error: "Search failed. Please try again.",
                Metadata: new Dictionary<string, object>
                {
                    ["query"] = request.Query,
                    ["error"] = ex.Message
                }
            );
        }
    }

    public async Task<SearchResult> SearchWithAiAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                _logger.LogWarning("AI search query is null or empty");
                return new SearchResult(new List<Chorus>(), 0);
            }

            _logger.LogInformation("Performing AI search with query: {Query}", request.Query);

            // For now, use the same search logic as regular search
            // In the future, this could integrate with vector search or LLM
            var result = await SearchAsync(request, cancellationToken);

            return result with
            {
                Metadata = result.Metadata?.Concat(new Dictionary<string, object>
                {
                    ["aiSearch"] = true,
                    ["searchType"] = "ai"
                }).ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object>
                {
                    ["aiSearch"] = true,
                    ["searchType"] = "ai"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI search with query: {Query}", request.Query);
            return new SearchResult(
                new List<Chorus>(), 
                0, 
                Error: "AI search failed. Please try again.",
                Metadata: new Dictionary<string, object>
                {
                    ["query"] = request.Query,
                    ["aiSearch"] = true,
                    ["error"] = ex.Message
                }
            );
        }
    }

    private static bool MatchesSearch(string text, string searchTerm, SearchMode searchMode)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return searchMode switch
        {
            SearchMode.Exact => string.Equals(text, searchTerm, StringComparison.OrdinalIgnoreCase),
            SearchMode.Contains => text.Contains(searchTerm, StringComparison.OrdinalIgnoreCase),
            SearchMode.Regex => IsRegexMatch(text, searchTerm),
            _ => false
        };
    }

    private static bool IsRegexMatch(string text, string pattern)
    {
        try
        {
            return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            // Invalid regex pattern
            return false;
        }
    }
} 