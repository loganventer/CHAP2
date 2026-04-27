using System.Net.Http.Json;
using System.Text.Json;
using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;

namespace CHAP2.WebPortal.Services;

public class UserPreferencesApiService : IUserPreferencesApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public UserPreferencesApiService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("CHAP2API");
    }

    public async Task<UserPreferencesDto?> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/me/preferences", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserPreferencesDto>(JsonOptions, cancellationToken);
    }

    public async Task<UserPreferencesDto?> UpdateMineAsync(string theme, string defaultSearchScope, string language, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync("/api/me/preferences", new UpdateUserPreferencesRequestDto
        {
            Theme = theme,
            DefaultSearchScope = defaultSearchScope,
            Language = language,
        }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserPreferencesDto>(JsonOptions, cancellationToken);
    }
}
