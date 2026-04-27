namespace CHAP2.WebPortal.Auth;

public sealed record StoredTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);
