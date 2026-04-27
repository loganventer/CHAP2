namespace CHAP2.WebPortal.Auth;

public static class AuthClaimTypes
{
    public const string AccessToken = "chap2:access_token";
    public const string RefreshToken = "chap2:refresh_token";
    public const string AccessTokenExpiresUtc = "chap2:access_token_expires_utc";
    public const string MustChangePassword = "chap2:must_change_password";
}
