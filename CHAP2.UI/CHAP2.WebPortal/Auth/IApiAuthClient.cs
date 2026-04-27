using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Auth;

public interface IApiAuthClient
{
    Task<ApiAuthOutcome> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiLoginOutcome> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<StoredTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> GetMeAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<ApiAuthOutcome> ChangePasswordAsync(string accessToken, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiAuthOutcome> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiAuthOutcome> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken cancellationToken = default);
}
