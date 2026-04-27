using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;

namespace CHAP2.WebPortal.Services;

public class SetlistApiService : ISetlistApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public SetlistApiService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("CHAP2API");
    }

    public async Task<IReadOnlyList<SetlistSummaryDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/setlists", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<SetlistSummaryDto>();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SetlistSummaryDto>>(JsonOptions, cancellationToken)
               ?? Array.Empty<SetlistSummaryDto>();
    }

    public async Task<SetlistDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/setlists/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<SetlistDto?> SaveByNameAsync(string name, IReadOnlyList<SetlistItemPayloadDto> items, CancellationToken cancellationToken = default)
    {
        var request = new SaveSetlistRequestDto { Name = name, Items = items ?? Array.Empty<SetlistItemPayloadDto>() };
        var response = await _http.PostAsJsonAsync("/api/setlists/save", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/setlists/{id}", cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
    }

    public async Task<SetlistDto?> GetWorkingDraftAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/setlists/working", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<SetlistDto?> SaveWorkingDraftAsync(IReadOnlyList<SetlistItemPayloadDto> items, CancellationToken cancellationToken = default)
    {
        var request = new SaveWorkingDraftRequestDto { Items = items ?? Array.Empty<SetlistItemPayloadDto>() };
        var response = await _http.PutAsJsonAsync("/api/setlists/working", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }
}
