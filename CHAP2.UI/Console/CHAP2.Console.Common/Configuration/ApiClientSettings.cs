using CHAP2.Shared.Configuration;

namespace CHAP2.Console.Common.Configuration;

public class ApiClientSettings
{
    public int TimeoutSeconds { get; set; } = SharedApiSettings.DefaultTimeoutSeconds;
    public int RetryAttempts { get; set; } = SharedApiSettings.DefaultRetryAttempts;
    public int RetryDelayMs { get; set; } = SharedApiSettings.DefaultRetryDelayMs;
} 