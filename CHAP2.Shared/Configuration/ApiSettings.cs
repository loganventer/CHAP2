namespace CHAP2.Shared.Configuration;

public class ApiSettings
{
    public string GlobalRoutePrefix { get; set; } = "api";
    public int DefaultPort { get; set; } = 5001;
    public string MaxRequestSize { get; set; } = "10MB";
    public string BaseUrl { get; set; } = "http://localhost:5001";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}
