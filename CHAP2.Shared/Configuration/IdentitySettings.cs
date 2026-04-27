namespace CHAP2.Shared.Configuration;

public class IdentitySettings
{
    public const string SectionName = "Identity";

    public string DatabasePath { get; set; } = "chap2.db";
    public string DataProtectionKeysPath { get; set; } = "keys";
    public string SeedAdminUserName { get; set; } = "ekvjc";
    public string SeedAdminEmail { get; set; } = "ekvjc@chap2.local";
    public string SeedAdminInitialPassword { get; set; } = string.Empty;
    /// <summary>
    /// One-shot escape hatch: when true, every startup resets the seed admin's
    /// password to <see cref="SeedAdminInitialPassword"/>, ensures the Admin
    /// role grant, and clears any lockout. Flip back to false after recovery.
    /// </summary>
    public bool ForceReseedAdmin { get; set; } = false;
    public TimeSpan BearerTokenExpiration { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshTokenExpiration { get; set; } = TimeSpan.FromDays(14);
}
