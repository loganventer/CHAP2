using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CHAP2.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Auth;

public class ApiAuthClient : IApiAuthClient
{
    public const string HttpClientName = "CHAP2API-Auth";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<ApiAuthClient> _logger;

    public ApiAuthClient(IHttpClientFactory httpClientFactory, ILogger<ApiAuthClient> logger)
    {
        _http = httpClientFactory.CreateClient(HttpClientName);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiAuthOutcome> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/identity/register", request, JsonOptions, cancellationToken);
        return await ToOutcomeAsync(response, cancellationToken);
    }

    public async Task<ApiLoginOutcome> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/identity/login", request, JsonOptions, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return ApiLoginOutcome.Fail("Invalid username or password.");
        if (response.StatusCode == (HttpStatusCode)423) return ApiLoginOutcome.Fail("Account is locked. Try again later.");
        if (!response.IsSuccessStatusCode) return ApiLoginOutcome.Fail($"Unexpected response {(int)response.StatusCode}.");

        var tokens = await ParseTokenResponseAsync(response, cancellationToken);
        if (tokens is null) return ApiLoginOutcome.Fail("Invalid token response from API.");

        var me = await GetMeAsync(tokens.AccessToken, cancellationToken);
        if (me is null) return ApiLoginOutcome.Fail("Could not load user profile after login.");

        return ApiLoginOutcome.Ok(tokens, me);
    }

    public async Task<StoredTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/identity/refresh",
            new RefreshTokenRequestDto { RefreshToken = refreshToken }, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await ParseTokenResponseAsync(response, cancellationToken);
    }

    public async Task<UserSummaryDto?> GetMeAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserSummaryDto>(JsonOptions, cancellationToken);
    }

    public async Task<ApiAuthOutcome> ChangePasswordAsync(string accessToken, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/identity/change-password")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(msg, cancellationToken);
        return await ToOutcomeAsync(response, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/identity/forgot-password", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("forgot-password returned {Status}", (int)response.StatusCode);
        }
    }

    public async Task<ApiAuthOutcome> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/identity/reset-password", request, JsonOptions, cancellationToken);
        return await ToOutcomeAsync(response, cancellationToken);
    }

    public async Task<ApiAuthOutcome> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("/identity/confirm-email", request, JsonOptions, cancellationToken);
        return await ToOutcomeAsync(response, cancellationToken);
    }

    private static async Task<StoredTokens?> ParseTokenResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        if (!json.TryGetProperty("accessToken", out var accessProp)) return null;
        var access = accessProp.GetString();
        if (string.IsNullOrEmpty(access)) return null;

        var refresh = json.TryGetProperty("refreshToken", out var refreshProp) ? refreshProp.GetString() ?? "" : "";
        var expiresIn = json.TryGetProperty("expiresIn", out var expProp) ? expProp.GetInt64() : 3600;
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        return new StoredTokens(access, refresh, expiresAt);
    }

    private static async Task<ApiAuthOutcome> ToOutcomeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return ApiAuthOutcome.Ok();

        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == (HttpStatusCode)422)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
                if (problem.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var prop in errors.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                            messages.AddRange(prop.Value.EnumerateArray().Select(v => v.GetString() ?? prop.Name));
                    }
                    return ApiAuthOutcome.Fail(messages.ToArray());
                }
                if (problem.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    return ApiAuthOutcome.Fail(detail.GetString() ?? "Bad request.");
            }
            catch { /* fall through to generic */ }
        }

        return ApiAuthOutcome.Fail($"Request failed ({(int)response.StatusCode}).");
    }
}
