namespace CHAP2.Shared.Configuration;

public class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Per-IP fixed window for anonymous auth endpoints (login, register,
    /// forgot/reset/confirm, refresh). Burst above this in a 1-minute
    /// window returns 429.
    /// </summary>
    public int AuthRequestsPerMinute { get; set; } = 10;

    /// <summary>
    /// Excess requests above the limit will queue up to this count before
    /// being rejected. Default 0 = reject immediately past the limit.
    /// </summary>
    public int AuthQueueLimit { get; set; } = 0;
}
