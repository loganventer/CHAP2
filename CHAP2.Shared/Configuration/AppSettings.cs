namespace CHAP2.Shared.Configuration;

/// <summary>
/// Centralized application settings following IDesign principles
/// </summary>
public class AppSettings
{
    public ApiSettings Api { get; set; } = new();
    public SearchSettings Search { get; set; } = new();
    public SlideConversionSettings SlideConversion { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public HttpClientSettings HttpClient { get; set; } = new();
}

public class ApiSettings
{
    public string GlobalRoutePrefix { get; set; } = "api";
    public int DefaultPort { get; set; } = 5000;
    public string MaxRequestSize { get; set; } = "10MB";
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class SearchSettings
{
    public int MaxResults { get; set; } = 50;
    public int MaxDisplayResults { get; set; } = 10;
    public string DefaultSearchMode { get; set; } = "Contains";
    public string DefaultSearchScope { get; set; } = "all";
}

public class SlideConversionSettings
{
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedExtensions { get; set; } = { ".pptx", ".ppsx" };
    public int MaxConcurrentConversions { get; set; } = 3;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = false;
    public string LogFilePath { get; set; } = "logs/chap2.log";
}

public class HttpClientSettings
{
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool EnableRetryPolicy { get; set; } = true;
} 