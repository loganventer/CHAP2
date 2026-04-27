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

    public async Task<IReadOnlyList<SetlistDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/setlists", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<SetlistDto>();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SetlistDto>>(JsonOptions, cancellationToken)
               ?? Array.Empty<SetlistDto>();
    }

    public async Task<SetlistDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/setlists/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<SetlistDto?> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/setlists", new CreateSetlistRequestDto { Name = name }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<SetlistDto?> RenameAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/setlists/{id}", new RenameSetlistRequestDto { Name = newName }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/setlists/{id}", cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
    }

    public async Task<SetlistDto?> AppendChorusAsync(Guid id, Guid chorusId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/setlists/{id}/items", new AppendChorusRequestDto { ChorusId = chorusId }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<SetlistDto?> RemoveItemAsync(Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/setlists/{id}/items/{itemId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }

    public async Task<SetlistDto?> ReorderAsync(Guid id, IReadOnlyList<Guid> itemIdsInOrder, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/setlists/{id}/reorder",
            new ReorderSetlistRequestDto { ItemIdsInOrder = itemIdsInOrder }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SetlistDto>(JsonOptions, cancellationToken);
    }
}
