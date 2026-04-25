using System.Net;
using System.Text.Json;
using CHAP2.Shared.Configuration;
using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;

namespace CHAP2.WebPortal.Services;

public class BibleApiService : IBibleApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BibleApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BibleApiService(IHttpClientFactory httpClientFactory, ILogger<BibleApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CHAP2API");
        _logger = logger;
    }

    public async Task<List<BibleBookDto>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/bible/books", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new List<BibleBookDto>();
        EnsureSuccess(response, "GetBooks");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<BibleBookDto>>(content, _jsonOptions) ?? new List<BibleBookDto>();
    }

    public async Task<BibleChapterDto?> GetChapterAsync(string bookId, int chapter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId) || chapter < 1)
            return null;

        var url = $"/api/bible/books/{Uri.EscapeDataString(bookId)}/chapters/{chapter}";
        var response = await SendAsync(HttpMethod.Get, url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        EnsureSuccess(response, "GetChapter");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<BibleChapterDto>(content, _jsonOptions);
    }

    public async Task<BibleSearchResponseDto> SearchAsync(string query, int max = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new BibleSearchResponseDto { Query = string.Empty, MaxResults = max };

        var url = $"/api/bible/search?q={Uri.EscapeDataString(query)}&max={max}";
        var response = await SendAsync(HttpMethod.Get, url, cancellationToken);
        EnsureSuccess(response, "Search");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<BibleSearchResponseDto>(content, _jsonOptions)
            ?? new BibleSearchResponseDto { Query = query, MaxResults = max };
    }

    public async Task<BibleReferenceDto?> ResolveReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var url = $"/api/bible/resolve?ref={Uri.EscapeDataString(reference)}";
        var response = await SendAsync(HttpMethod.Get, url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        EnsureSuccess(response, "Resolve");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<BibleReferenceDto>(content, _jsonOptions);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogWarning(ex, "Bible API unreachable for {Method} {Url}", method.Method, url);
            throw new ApiUnavailableException($"Bible API unreachable for {method.Method} {url}.", ex);
        }
    }

    private void EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (response.IsSuccessStatusCode)
            return;
        _logger.LogWarning("Bible API {Action} returned {Status}", action, (int)response.StatusCode);
        throw new ApiUnavailableException($"Bible API responded {(int)response.StatusCode} for {action}.");
    }
}
