namespace CHAP2.Shared.Configuration;

public class SharedApiSettings
{
    public const string DefaultApiBaseUrl = "http://localhost:5001";
    // 75s per attempt is long enough to ride out a Render.com free-tier
    // cold start (the API can take 30-60s to wake from idle) without
    // tripping the HttpClient timeout before the API answers.
    public const int DefaultTimeoutSeconds = 75;
    public const int DefaultRetryAttempts = 3;
    public const int DefaultRetryDelayMs = 1500;
    public const int DefaultMaxConcurrentConversions = 3;
    public const int DefaultMaxDisplayResults = 10;
    
    public string ApiBaseUrl { get; set; } = DefaultApiBaseUrl;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
    public int RetryAttempts { get; set; } = DefaultRetryAttempts;
    public int RetryDelayMs { get; set; } = DefaultRetryDelayMs;
    public int MaxConcurrentConversions { get; set; } = DefaultMaxConcurrentConversions;
    public int MaxDisplayResults { get; set; } = DefaultMaxDisplayResults;
} 