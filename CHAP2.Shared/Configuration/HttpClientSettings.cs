namespace CHAP2.Shared.Configuration;

public class HttpClientSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool EnableRetryPolicy { get; set; } = true;
}
