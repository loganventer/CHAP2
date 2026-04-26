using CHAP2.Shared.Configuration;
using CHAP2.WebPortal.Interfaces;

namespace CHAP2.WebPortal.Services;

public sealed class SyncApiService : ISyncApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SyncApiService> _logger;

    public SyncApiService(IHttpClientFactory httpClientFactory, ILogger<SyncApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CHAP2API");
        _logger = logger;
    }

    public async Task ForceSyncAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sync/force");
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            _logger.LogWarning(ex, "Sync API unreachable for force sync");
            throw new ApiUnavailableException("Sync API unreachable for force sync.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sync API force returned {Status}", (int)response.StatusCode);
                throw new ApiUnavailableException($"Sync API responded {(int)response.StatusCode} for force sync.");
            }
            await using var apiStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await apiStream.CopyToAsync(destination, cancellationToken);
        }
    }
}
