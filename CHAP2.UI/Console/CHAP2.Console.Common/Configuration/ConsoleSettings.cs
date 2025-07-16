using CHAP2.Shared.Configuration;

namespace CHAP2.Console.Common.Configuration;

public class ConsoleSettings
{
    public int MaxDisplayResults { get; set; } = SharedApiSettings.DefaultMaxDisplayResults;
    public bool ClearScreenOnSearch { get; set; } = true;
    public bool ShowSearchDelay { get; set; } = true;
    public bool ShowMinSearchLength { get; set; } = true;
    public bool ForceFallbackInputMode { get; set; } = false;
} 