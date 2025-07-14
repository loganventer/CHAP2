namespace CHAP2.Console.Common.Configuration;

public class ApiClientSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
} 