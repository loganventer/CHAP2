using CHAP2.Shared.Configuration;

namespace CHAP2.Console.Common.Configuration;

public class BulkConversionSettings
{
    public int MaxConcurrentConversions { get; set; } = SharedApiSettings.DefaultMaxConcurrentConversions;
    public int RetryAttempts { get; set; } = 2;
    public int RetryDelayMs { get; set; } = SharedApiSettings.DefaultRetryDelayMs;
    public bool ShowProgress { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
} 