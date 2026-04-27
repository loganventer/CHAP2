namespace CHAP2.WebPortal.Auth;

public class ApiAuthSettings
{
    public const string SectionName = "ApiAuth";

    public string CookieName { get; set; } = "chap2-auth";
    public TimeSpan CookieLifetime { get; set; } = TimeSpan.FromDays(30);
    public string DataProtectionKeysPath { get; set; } = string.Empty;
}
