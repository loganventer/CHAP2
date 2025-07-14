namespace CHAP2.Console.Common.Configuration;

public class BulkConversionSettings
{
    public int MaxConcurrentConversions { get; set; } = 3;
    public int RetryAttempts { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 1000;
    public bool ShowProgress { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
} 