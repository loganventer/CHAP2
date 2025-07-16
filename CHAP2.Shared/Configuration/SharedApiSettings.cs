namespace CHAP2.Shared.Configuration;

public class SharedApiSettings
{
    public const string DefaultApiBaseUrl = "https://localhost:7000";
    public const int DefaultTimeoutSeconds = 30;
    public const int DefaultRetryAttempts = 3;
    public const int DefaultRetryDelayMs = 1000;
    public const int DefaultMaxConcurrentConversions = 3;
    public const int DefaultMaxDisplayResults = 10;
    
    public string ApiBaseUrl { get; set; } = DefaultApiBaseUrl;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
    public int RetryAttempts { get; set; } = DefaultRetryAttempts;
    public int RetryDelayMs { get; set; } = DefaultRetryDelayMs;
    public int MaxConcurrentConversions { get; set; } = DefaultMaxConcurrentConversions;
    public int MaxDisplayResults { get; set; } = DefaultMaxDisplayResults;
} 