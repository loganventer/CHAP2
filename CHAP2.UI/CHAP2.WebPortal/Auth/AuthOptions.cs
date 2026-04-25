namespace CHAP2.WebPortal.Auth;

/// <summary>
/// Single-user portal credentials. Lives in configuration so the
/// password is never embedded in served HTML/JS. PasswordHash is the
/// PBKDF2 string produced by Pbkdf2PasswordHasher (format:
/// "pbkdf2-sha256$&lt;iterations&gt;$&lt;saltB64&gt;$&lt;keyB64&gt;").
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
