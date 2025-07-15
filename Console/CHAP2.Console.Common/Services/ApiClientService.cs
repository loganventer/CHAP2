using System.Text.Json;
using CHAP2.Console.Common.Interfaces;
using CHAP2.Common.Models;
using CHAP2.Console.Common.Configuration;
using CHAP2.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Console.Common.Services;

public class ApiClientService : IApiClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiClientService> _logger;
    private readonly ISearchCacheService _cache;
    private readonly ConsoleApiSettings _settings;

    public ApiClientService(IHttpClientFactory httpClientFactory, ILogger<ApiClientService> logger, ISearchCacheService cache, IConfigurationService configService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
        _settings = configService.GetConfiguration<ConsoleApiSettings>("ConsoleApiSettings");
    }

    public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("CHAP2API");
            _logger.LogInformation("Testing API connectivity to: {BaseAddress}", httpClient.BaseAddress);
            
            if (httpClient.BaseAddress == null)
            {
                _logger.LogError("HttpClient BaseAddress is null! This should not happen.");
                return false;
            }
            
            // Add a timeout to prevent hanging
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.ConnectivityTestTimeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            var response = await httpClient.GetAsync("/api/health/ping", combinedCts.Token);
            _logger.LogInformation("API connectivity test response: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("API connectivity test was cancelled by user");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("API connectivity test timed out after {TimeoutSeconds} seconds", _settings.ConnectivityTestTimeoutSeconds);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test API connectivity");
            return false;
        }
    }

    public async Task<Chorus?> ConvertSlideAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return null;
            }

            if (!filePath.EndsWith(".ppsx", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Only .ppsx files are supported: {FilePath}", filePath);
                return null;
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var fileName = Path.GetFileName(filePath);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/slide/convert");
            request.Content = new ByteArrayContent(fileBytes);
            request.Headers.Add("X-Filename", fileName);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClientFactory.CreateClient("CHAP2API").SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (responseJson.TryGetProperty("chorus", out var chorusObj))
                {
                    return JsonSerializer.Deserialize<Chorus>(chorusObj.GetRawText());
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Slide conversion failed: {Error}", errorContent);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert slide: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<List<Chorus>> SearchChorusesAsync(string searchTerm, string searchMode = "Contains", string searchIn = "all", CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cache.TryGet(searchTerm, out var cachedResults))
            {
                _logger.LogInformation("Cache hit for search term: {SearchTerm}", searchTerm);
                return cachedResults;
            }
            var url = $"api/choruses/search?q={Uri.EscapeDataString(searchTerm)}&searchIn={searchIn}&searchMode={searchMode}";
            var response = await _httpClientFactory.CreateClient("CHAP2API").GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Search API response: {Content}", content);
                var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (searchResult.TryGetProperty("results", out var results))
                {
                    _logger.LogInformation("Raw results JSON: {ResultsJson}", results.GetRawText());
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var choruses = JsonSerializer.Deserialize<List<Chorus>>(results.GetRawText(), options) ?? new List<Chorus>();
                    _logger.LogInformation("Deserialized {Count} choruses from search results", choruses.Count);
                    foreach (var chorus in choruses)
                    {
                        _logger.LogInformation("Chorus: Id={Id}, Name='{Name}', Type={Type}", chorus?.Id, chorus?.Name, chorus?.Type);
                    }
                    _cache.Set(searchTerm, choruses, TimeSpan.FromMinutes(_settings.CacheDurationMinutes));
                    return choruses;
                }
                else
                {
                    _logger.LogWarning("No 'results' property found in search response");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Search failed: {Error}", errorContent);
            }

            return new List<Chorus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search choruses: {SearchTerm}", searchTerm);
            return new List<Chorus>();
        }
    }

    public async Task<Chorus?> GetChorusByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClientFactory.CreateClient("CHAP2API").GetAsync($"api/choruses/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<Chorus>(content);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chorus by ID: {Id}", id);
            return null;
        }
    }

    public async Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClientFactory.CreateClient("CHAP2API").GetAsync($"api/choruses/by-name/{Uri.EscapeDataString(name)}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<Chorus>(content);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chorus by name: {Name}", name);
            return null;
        }
    }

    public async Task<List<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClientFactory.CreateClient("CHAP2API").GetAsync("api/choruses", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<Chorus>>(content) ?? new List<Chorus>();
            }

            return new List<Chorus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all choruses");
            return new List<Chorus>();
        }
    }
} 