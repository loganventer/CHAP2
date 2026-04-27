namespace CHAP2.Chorus.Api.RateLimiting;

public static class RateLimitPolicyNames
{
    /// <summary>Per-IP throttle for unauthenticated auth endpoints.</summary>
    public const string AuthAnonymous = "auth-anonymous";
}
