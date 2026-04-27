using System.Net.Http.Json;
using System.Text.Json;
using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;

namespace CHAP2.WebPortal.Services;

public class UserSettingsApiService : IUserSettingsApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public UserSettingsApiService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("CHAP2API");
    }

    public async Task<UserSettingsDto?> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/me/settings", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions, cancellationToken);
    }

    public async Task<UserSettingsDto?> SaveMineAsync(string json, CancellationToken cancellationToken = default)
    {
        var request = new SaveUserSettingsRequestDto { Json = json ?? string.Empty };
        var response = await _http.PutAsJsonAsync("/api/me/settings", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions, cancellationToken);
    }
}
