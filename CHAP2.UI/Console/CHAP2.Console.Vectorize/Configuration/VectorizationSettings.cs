namespace CHAP2.Console.Vectorize.Configuration;

public class VectorizationSettings
{
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
} 