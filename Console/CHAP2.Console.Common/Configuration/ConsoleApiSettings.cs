namespace CHAP2.Console.Common.Configuration;

public class ConsoleApiSettings
{
    public int CacheDurationMinutes { get; set; } = 10;
    public int ConnectivityTestTimeoutSeconds { get; set; } = 10;
} 