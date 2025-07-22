using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace CHAP2.Application.Services;

public class ChorusSearchService : ISearchService
{
    private readonly IChorusRepository _chorusRepository;
    private readonly ILogger<ChorusSearchService> _logger;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
    private readonly IAiSearchService _aiSearchService;

    private const string AllChorusesCacheKey = "AllChoruses";
    private const int CacheDurationMinutes = 15;

    public ChorusSearchService(
        IChorusRepository chorusRepository,
        ILogger<ChorusSearchService> logger,
        IMemoryCache cache,
        IAiSearchService aiSearchService)
    {
        _chorusRepository = chorusRepository;
        _logger = logger;
        _cache = cache;
        _aiSearchService = aiSearchService;
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

            // Step 1: Use AI to generate search terms and context
            var aiSearchTerms = await _aiSearchService.GenerateSearchTermsAsync(request.Query, cancellationToken);
            
            // Step 2: Perform vector search with AI-generated terms
            var vectorResults = await PerformVectorSearchWithAiTermsAsync(request.Query, aiSearchTerms, request.MaxResults, cancellationToken);
            
            // Step 3: Combine with traditional search for comprehensive results
            var traditionalResults = await SearchAsync(request, cancellationToken);
            
            // Step 4: Merge and rank results
            var combinedResults = MergeAndRankResults(vectorResults, traditionalResults.Results, request.Query);
            
            var limitedResults = combinedResults.Take(request.MaxResults).ToList();

            _logger.LogInformation("AI search completed. Found {TotalCount} results, returning {LimitedCount}", 
                combinedResults.Count, limitedResults.Count);

            return new SearchResult(
                limitedResults,
                combinedResults.Count,
                Metadata: new Dictionary<string, object>
                {
                    ["query"] = request.Query,
                    ["aiSearch"] = true,
                    ["searchType"] = "ai",
                    ["aiSearchTerms"] = aiSearchTerms,
                    ["vectorResultsCount"] = vectorResults.Count,
                    ["traditionalResultsCount"] = traditionalResults.Results.Count,
                    ["combinedResultsCount"] = combinedResults.Count,
                    ["searchContext"] = await _aiSearchService.AnalyzeSearchContextAsync(request.Query, aiSearchTerms, cancellationToken)
                }
            );
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



    private async Task<List<Chorus>> PerformVectorSearchWithAiTermsAsync(string originalQuery, List<string> searchTerms, int maxResults, CancellationToken cancellationToken)
    {
        var allResults = new List<Chorus>();
        
        // Use the original query for vector search (most relevant)
        var vectorResults = await SearchAllAsync(originalQuery, SearchMode.Contains, cancellationToken);
        allResults.AddRange(vectorResults);
        
        // Add results from AI-generated search terms
        foreach (var term in searchTerms.Take(3)) // Limit to top 3 terms to avoid too many results
        {
            if (term != originalQuery) // Avoid duplicate searches
            {
                var termResults = await SearchAllAsync(term, SearchMode.Contains, cancellationToken);
                allResults.AddRange(termResults);
            }
        }

        // Remove duplicates and rank by relevance
        var uniqueResults = allResults
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .OrderByDescending(c => 
            {
                var content = $"{c.Name} {c.ChorusText}".ToLowerInvariant();
                var score = 0;
                
                // Original query gets highest weight
                if (content.Contains(originalQuery.ToLowerInvariant())) score += 10;
                
                // AI-generated terms get medium weight
                foreach (var term in searchTerms)
                {
                    if (content.Contains(term.ToLowerInvariant())) score += 3;
                }
                
                // Exact matches get bonus
                if (c.Name.ToLowerInvariant().Contains(originalQuery.ToLowerInvariant())) score += 5;
                
                return score;
            })
            .Take(maxResults)
            .ToList();

        _logger.LogInformation("Vector search with AI terms found {Count} unique results", uniqueResults.Count);
        return uniqueResults;
    }

    private List<Chorus> MergeAndRankResults(List<Chorus> vectorResults, IReadOnlyList<Chorus> traditionalResults, string query)
    {
        var allResults = new List<Chorus>();
        
        // Add vector results with higher weight
        allResults.AddRange(vectorResults);
        
        // Add traditional results that aren't already included
        var existingIds = vectorResults.Select(r => r.Id).ToHashSet();
        allResults.AddRange(traditionalResults.Where(r => !existingIds.Contains(r.Id)));
        
        // Rank by relevance to original query
        var queryLower = query.ToLowerInvariant();
        return allResults
            .OrderByDescending(c => 
            {
                var content = $"{c.Name} {c.ChorusText}".ToLowerInvariant();
                var score = 0;
                
                // Exact match gets highest score
                if (content.Contains(queryLower)) score += 10;
                
                // Word matches
                var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in queryWords)
                {
                    if (content.Contains(word)) score += 2;
                }
                
                // Vector results get bonus
                if (vectorResults.Any(v => v.Id == c.Id)) score += 5;
                
                return score;
            })
            .ToList();
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