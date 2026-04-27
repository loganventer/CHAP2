using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CHAP2.Shared.DTOs;
using CHAP2.WebPortal.Interfaces;

namespace CHAP2.WebPortal.Services;

public class UserAdminApiService : IUserAdminApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public UserAdminApiService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("CHAP2API");
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/users", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<UserSummaryDto>();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<UserSummaryDto>>(JsonOptions, cancellationToken)
               ?? Array.Empty<UserSummaryDto>();
    }

    public async Task<UserSummaryDto?> AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/users/{userId}/roles", new AssignRoleRequestDto { Role = role }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserSummaryDto>(JsonOptions, cancellationToken);
    }

    public async Task<UserSummaryDto?> RevokeRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/users/{userId}/roles/{role}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserSummaryDto>(JsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/users/{userId}", cancellationToken);
        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
    }
}
