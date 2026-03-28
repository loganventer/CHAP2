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
